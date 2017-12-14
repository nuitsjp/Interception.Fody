echo off
copy /Y ..\Weaving.Fody\bin\Debug\Weaving.dll .
copy /Y ..\Weaving.Fody\bin\Debug\Weaving.pdb .
copy /Y ..\Weaving.Fody\bin\Debug\Weaving.Fody.dll .
copy /Y ..\Weaving.Fody\bin\Debug\Weaving.Fody.pdb .
nuget.exe pack Weaving.Fody.nuspec -Exclude nuget.exe -Exclude package.cmd
xcopy /Y . ..\packages\Weaving.Fody.1.0.0