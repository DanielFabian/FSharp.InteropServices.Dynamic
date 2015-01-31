module FSharp.Dynamic.DlrHelper

open System.Linq.Expressions
open System
open System.Dynamic
open System.Runtime.CompilerServices

type PrintExpression(text:ConstantExpression) =
    inherit Expression()

    let methodInfo = typeof<Console>.GetMethod("WriteLine", [| typeof<string> |])

    member x.Text = text
    override x.CanReduce = true
    override x.Reduce() = upcast Expression.Call(null, methodInfo, text)
    override x.Type = methodInfo.ReturnType
    override x.ToString() = sprintf "print %O" text.Value
    override x.NodeType = ExpressionType.Extension

module Patterns =
    let (|Print|_|) : Expression -> _ = function
        | :? PrintExpression as e -> Some e.Text
        | _ -> None

    let (|Constant|_|) : Expression -> 'a option = function
        | :? ConstantExpression as e when e.Value <> null ->
            match e.Value with
            | :? 'a as value -> Some value
            | _ -> None
        | _ -> None

module Expression =
    let Print text = PrintExpression(text)

let myProgram =
    let x = Expression.Variable(typeof<int>, "x")
    let y = Expression.Variable(typeof<int>, "y")
    Expression.Block(typeof<System.Void>, [x; y], Expression.Print(Expression.Constant "Hello World"))

let visitor =
    { new ExpressionVisitor() with
        member x.VisitExtension node =
            match node with
            | Patterns.Print (Patterns.Constant msg) -> upcast Expression.Print(Expression.Constant (sprintf "printing: %s" msg))
            | node -> node }

let myProgram' = visitor.Visit myProgram

let lambda = Expression.Lambda<Action> myProgram'
lambda.Compile().Invoke()

type PInvokeMemberBinder(name, callInfo) =
    inherit InvokeMemberBinder(name, false, callInfo)
    
    override x.FallbackInvokeMember(target, args, errorSuggestion) = 
        printfn "fallbackInvokeMember"
        if obj.ReferenceEquals(errorSuggestion, null) then
            DynamicMetaObject(Expression.Convert(Expression.Constant 3, typeof<obj>), BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
        else
            errorSuggestion

    override x.FallbackInvoke(target, args, errorSuggestion) = 
        base.FallbackInvoke(target, args, errorSuggestion)

type PInvokeCallSiteBinder(methodName) =
    inherit CallSiteBinder()
    override x.Bind(args, parameters, returnLabel) =
        printfn "cache miss: %s%A" methodName args
        printfn "args: %A, return: %A" parameters returnLabel.Type
        upcast Expression.Return(returnLabel, Expression.Constant "bla")

    static member Create(lib, name) = ()
        

