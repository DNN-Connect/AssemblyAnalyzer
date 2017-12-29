# AssemblyAnalyzer

Powershell module to analyze a built assembly and output an XML file with all details which can be used by the Api Browser project

## Syntax

Analyze-Assembly [Path to Assembly] [Path to Source Code] [Output Path]

## What does it do?

This task will decompile the assembly (using the ICSharp Decompiler) and enumerate through all classes and members of those 
classes and extract the original source code from the source code location (i.e. with any inline comments that were written
and in the original formatting). It will also attempt to read any XML comments above the method from the source code and parse
that. Finally it will detect the deprecation flag and message. All this information is written out to an XML file. This XML file
is used by the ApiBrowser project to load and compare with previous versions to see how the code has changed over time
for a particular project.

## Technology

This project is targeting Dotnet Standard 2.0! For it to work you'll need to have the .net framework 4.7.1 installed and 
Powershell 5.1.

## Installing

Compile to a location you wish and announce the module in your Powershell script using "Add-Module [Path to module DLL]".

