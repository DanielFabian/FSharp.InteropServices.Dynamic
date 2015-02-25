module FSharp.InteropServices.Dynamic

open System
open System.Collections.Concurrent
open System.Reflection
open System.Linq.Expressions
open System.Reflection.Emit
open System.Threading
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open Microsoft.FSharp.Reflection

type Library(name, ?charSet, ?callingConvention) =
    let charSet = defaultArg charSet CharSet.Auto
    let callingConvention = defaultArg callingConvention CallingConvention.Cdecl
    let assemblyName = lazy AssemblyName(IO.Path.GetFileNameWithoutExtension name) 
    let assemblyBuilder = lazy AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName.Value, AssemblyBuilderAccess.Run)
    let moduleBuilder = lazy assemblyBuilder.Value.DefineDynamicModule assemblyName.Value.Name
    let methodIndex = ref 0
    let defineTypeName methodName = sprintf "%s_%d" methodName (Interlocked.Increment methodIndex)
    member this.Name = name
    member this.GetInvokeMethod(methodName, returnType, types) : MethodInfo = 
        let defineType = moduleBuilder.Value.DefineType(defineTypeName methodName)
        let methodBuilder =
            defineType.DefinePInvokeMethod(
                methodName, name, methodName, MethodAttributes.Public ||| MethodAttributes.Static ||| MethodAttributes.PinvokeImpl,
                CallingConventions.Standard, returnType, types, callingConvention, charSet)

        if returnType <> typeof<System.Void> then
            methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig ||| methodBuilder.GetMethodImplementationFlags())

        defineType.CreateType().GetMethod(methodName, BindingFlags.Public ||| BindingFlags.Static)

type PInvokeCallSiteBinder(methodName) =
    inherit CallSiteBinder()

    let asByRef (ty:Type) =
        if not ty.IsGenericType then ty else 
        match ty.GetGenericTypeDefinition() with
        | tyd when tyd = typedefof<_ ref> -> ty.GetGenericArguments().[0].MakeByRefType()
        | _ -> ty
        
        

    let rec expandTuple (expression:Expression) =
        if expression.Type = typeof<unit> then
            [| |]
        elif not expression.Type.IsGenericType then
            [|expression|]
        else
            let item i = Expression.Property(expression, if i = 8 then "Rest" else sprintf "Item%d" i)
            match expression.Type.GetGenericTypeDefinition() with
            | ty when ty = typedefof<_*_> -> [| item 1; item 2 |]
            | ty when ty = typedefof<_*_*_> -> [| item 1; item 2; item 3 |]
            | ty when ty = typedefof<_*_*_*_> -> [| item 1; item 2; item 3; item 4 |]
            | ty when ty = typedefof<_*_*_*_*_> -> [| item 1; item 2; item 3; item 4; item 5 |]
            | ty when ty = typedefof<_*_*_*_*_*_> -> [| item 1; item 2; item 3; item 4; item 5; item 6 |]
            | ty when ty = typedefof<_*_*_*_*_*_*_> -> [| item 1; item 2; item 3; item 4; item 5; item 6; item 7 |]
            | ty when ty = typedefof<_*_*_*_*_*_*_*_> -> Array.append [| item 1; item 2; item 3; item 4; item 5; item 6; item 7 |] (expandTuple (item 8))
            | ty when ty = typedefof<_ ref> -> [| Expression.Property(expression, "Value") |]
            | ty -> failwithf "unknown generic type: %A" ty


    override x.Bind(args, parameters, returnLabel) =
        let target, _, targetParam, argsParam = 
            match args, List.ofSeq parameters with
            | [| :? Library as target; args |], [targetParam; argsParam] -> target, args, targetParam, argsParam
            | _ -> failwithf "wrong amount of arguments: %A" args

        let argTypes =
            match FSharpType.IsTuple argsParam.Type with
            | true -> FSharpType.GetTupleElements argsParam.Type
            | false -> [| argsParam.Type |]
            |> Array.filter ((<>) typeof<unit>)
            |> Array.map asByRef

        match returnLabel.Type with
        | ty when FSharpType.IsFunction ty -> failwithf "Curried functions are not supported, use tupled arguments."
        | _ -> () // TODO: consider other evil cases, e.g. tuples, records, etc.

        let returnType = if returnLabel.Type = typeof<unit> then typeof<System.Void> else returnLabel.Type

        
        let mi = target.GetInvokeMethod(methodName, returnType, argTypes)
        
        //printfn "cache miss; PInvoke method: %A" mi

        if returnType = typeof<System.Void> then
            Expression.IfThen(
                Expression.TypeEqual(targetParam, typeof<Library>),
                Expression.Return(returnLabel,
                    Expression.Block(
                        Expression.Call(mi, expandTuple argsParam),
                        Expression.Default(typeof<unit>)))) :> _
        else
            Expression.IfThen(
                Expression.TypeEqual(targetParam, typeof<Library>),
                Expression.Return(returnLabel, Expression.Call(mi, expandTuple argsParam))) :> _

module Binder =
    let private binders = ConcurrentDictionary<_, PInvokeCallSiteBinder>()
    let PInvoke (lib:Library) methodName = binders.GetOrAdd((lib.Name, methodName), fun (_, methodName) -> PInvokeCallSiteBinder(methodName))

let (?) lib methodName : 'args -> 'res = 
    let binder = PInvokeCallSiteBinder(methodName)
    let binder = Binder.PInvoke lib methodName
    let callsite = CallSite<Func<CallSite, _, _, _>>.Create(binder)
    fun args -> callsite.Target.Invoke(callsite, lib, args)