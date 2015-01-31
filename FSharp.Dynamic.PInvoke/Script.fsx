// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "PInvoke.fs"

open System

open FSharp.Dynamic.PInvoke
open System.Text
// Define your library scripting code here

let user32 = Library("user32.dll")


let test =     
    let builder = StringBuilder("Hello World")

    user32?CharLower builder
    builder.ToString() |> printf "%s"

    for i = 0 to 10000 do
        (user32?CharLower 'B' : char) |> ignore

let ntdll = Library("ntdll.dll")

let test2 =
    let res = ref 0L
    ntdll?NtQuerySystemTime res
    DateTime.FromFileTime !res |> printfn "%A"

let test3 : int =

    user32?MessageBox(0, "Hello world", "MyTitle", 0)
    user32?MessageBox(0, "Hello world", "MyTitle", 0)
