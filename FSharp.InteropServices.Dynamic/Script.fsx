#load "PInvoke.fs"

open System

open FSharp.InteropServices.Dynamic
open System.Text

let (++) x y = System.IO.Path.Combine(x, y)

let user32 = Library("user32.dll")

let test =     
    let builder = StringBuilder("Hello World")

    user32?CharLower builder
    builder.ToString() |> printfn "%s"

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

let testDll =
    match IntPtr.Size with
    | 4 -> Library(__SOURCE_DIRECTORY__ ++ ".." ++ "Debug" ++ "TestDll.dll")
    | _ -> Library(__SOURCE_DIRECTORY__ ++ ".." ++ "x64" ++ "Debug" ++ "TestDll.dll")

let test4 : unit =
    let forty, two = ref 0, ref 0
    testDll?setArgs(forty, two)

    printfn "%A" (forty, two)