#load "PInvoke.fs"

open System

open FSharp.InteropServices.Dynamic
open System.Text

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
