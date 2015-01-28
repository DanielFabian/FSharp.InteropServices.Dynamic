// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "PInvoke.fs"
#I "../FSharp.Dynamic/bin/Debug"
#r "FSharp.Dynamic"
open FSharp.Dynamic
open FSharp.Dynamic.PInvoke

// Define your library scripting code here

let user32 = PInvoke("user32.dll", callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi)

let res : int = user32?MessageBox(0, "Hello world", "MyTitle", 0)
