namespace FSharp.Dynamic.PInvoke

open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.Dynamic
open System.Reflection
open System.Linq.Expressions
open System.Reflection.Emit
open System.Threading
open System.Runtime.InteropServices

type internal PInvokeMetaObject(expression, value) =
    inherit DynamicMetaObject(expression, BindingRestrictions.Empty, value)

    let returnType binder =
        binder.GetType().GetField("m_typeArguments", BindingFlags.NonPublic ||| BindingFlags.Instance).GetValue(binder) :?> _ seq
        |> Seq.toList
        |> function
           | returnType::_ -> returnType
           | _ -> null

    override this.BindInvokeMember(binder, args) = 
        let returnType = returnType binder
        let types, arguments =
            args
            |> Array.map (fun arg ->
                let expr = arg.Expression
                let ty =
                    match expr with
                    | :? ParameterExpression as p when p.IsByRef -> arg.LimitType.MakeByRefType()
                    | _ -> arg.LimitType
                ty, Expression.Convert(expr, ty) :> Expression)
            |> Array.unzip
        
        let dllImport = base.Value :?> PInvoke 
        let mi = dllImport.GetInvokeMethod(binder.Name, returnType, types)
        
        printfn "%A" returnType
        
        let call : Expression =
            if mi.ReturnType = typeof<System.Void> then
                upcast Expression.Block(Expression.Call(mi, arguments), Expression.Default(typeof<obj>))
            else
                upcast Expression.Convert(Expression.Call(mi, arguments), typeof<obj>)

        let restrictions = BindingRestrictions.GetTypeRestriction(this.Expression, typeof<PInvoke>)
        
        DynamicMetaObject(call, restrictions)
        
and PInvoke(dllName, ?charSet, ?callingConvention) =
    inherit DynamicObject()
    let charSet = defaultArg charSet CharSet.Auto
    let callingConvention = defaultArg callingConvention CallingConvention.Cdecl
    let name = lazy AssemblyName(IO.Path.GetFileNameWithoutExtension dllName) 
    let assemblyBuilder = lazy AppDomain.CurrentDomain.DefineDynamicAssembly(name.Value, AssemblyBuilderAccess.Run)
    let moduleBuilder = lazy assemblyBuilder.Value.DefineDynamicModule name.Value.Name
    let methodIndex = ref 0
    let defineTypeName methodName = sprintf "%s_%d" methodName (Interlocked.Increment methodIndex)
    override this.GetMetaObject(args) = upcast PInvokeMetaObject(args, this)
    member this.GetInvokeMethod(methodName, returnType, types) : MethodInfo = 
        let defineType = moduleBuilder.Value.DefineType(defineTypeName methodName)
        let methodBuilder =
            defineType.DefinePInvokeMethod(
                methodName, dllName, methodName, MethodAttributes.Public ||| MethodAttributes.Static ||| MethodAttributes.PinvokeImpl,
                CallingConventions.Standard, returnType, types, callingConvention, charSet)

        if returnType <> typeof<System.Void> then
            methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig ||| methodBuilder.GetMethodImplementationFlags())

        defineType.CreateType().GetMethod(methodName, BindingFlags.Public ||| BindingFlags.Static)
