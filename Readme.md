[![Issue Stats](http://issuestats.com/github/DanielFabian/FSharp.InteropServices.Dynamic/badge/issue)](http://issuestats.com/github/DanielFabian/FSharp.InteropServices.Dynamic)
[![Issue Stats](http://issuestats.com/github/DanielFabian/FSharp.InteropServices.Dynamic/badge/pr)](http://issuestats.com/github/DanielFabian/FSharp.InteropServices.Dynamic)

# FSharp.InteropServices.Dynamic [![NuGet Status](http://img.shields.io/nuget/v/FSharp.InteropServices.Dynamic.svg?style=flat)](https://www.nuget.org/packages/FSharp.InteropServices.Dynamic/)


F# Dynamic Operator for P/Invoke using the DLR

Install from [nuget](https://nuget.org/packages/FSharp.InteropServices.Dynamic/)
```
PM> Install-Package FSharp.InteropServices.Dynamic
```
Or simply copy the `PInvoke.fs` file to your project.

It is particularly well-suited for exploring libraries in F# scripts.

# Usage

Create a late-bound (dynamic) P/Invoke wrapper for an assembly by first creating an object `Library("mylib.dll")`. Then you can immediately access P/Invoke methods on it by using the `?` operator; `mylib?MyMethod("arg1", 2, 3.0)`

	open FSharp.InteropServices.Dynamic

	let user32 = Library("user32.dll")
    (user32?MessageBox(0, "Hello world", "MyTitle", 0) : unit)

#Status:

This library is in alpha status. Please give it a try. Feedback is very welcome.
