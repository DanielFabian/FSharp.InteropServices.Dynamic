#r "packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.ReleaseNotesHelper
open Fake.AssemblyInfoFile
open Fake.Git

let buildDir = "./build"
let nugetDir = "./nuget"

let release = LoadReleaseNotes "RELEASE_NOTES.md"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; nugetDir]
)

Target "Build" (fun _ ->
    !! "*.sln"
    |> MSBuildRelease buildDir "Build"
    |> Log "AppBuild-Output: "
)


Target "SetAssemblyInfo" (fun _ ->
    [ Attribute.Product "FSharp.InteropServices.Dynamic"
      Attribute.Version release.AssemblyVersion
      Attribute.InformationalVersion release.AssemblyVersion
      Attribute.FileVersion release.AssemblyVersion ]
    |> CreateFSharpAssemblyInfo "./FSharp.InteropServices.Dynamic/AssemblyInfo.fs"
)

Target "Nuget" (fun _ ->
    let libDir = nugetDir @@ "lib" @@ "net45"
    ensureDirectory libDir
    !! "*"
    -- "FSharp.Core.*"
    -- "*.pdb"
    |> SetBaseDir buildDir
    |> CopyFiles libDir

    let nuspec = !! "*.nuspec" |> Seq.exactlyOne

    printfn "%A" (getBuildParamOrDefault "nugetkey" "")

    NuGet (fun p -> 
        { p with 
            Authors = ["Daniel Fabian"]
            Project = "FSharp.InteropServices.Dynamic"
            Description = "DLR-based late-bound P/Invoke for F#"
            OutputPath = nugetDir
            Version = release.NugetVersion
            Summary =
                "Using the DLR, it is possible to concisely access native libraries in " +
                "scripts and programs. This project leverages the dynamic lookup operator of F# and " +
                "the dynamic language runtime to provide dynamic P/Invoke wrappers for native libraries."
            ReleaseNotes = release.Notes |> toLines
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" })
        nuspec
)

Target "Release" (fun _ ->
    StageAll ""
    Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)

Target "Default" DoNothing

"Clean"
    ==> "SetAssemblyInfo"
    ==> "Build"
    ==> "Nuget"
    ==> "Default"
    ==> "Release"

RunTarget()