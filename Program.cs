﻿using System;
using System.Windows.Forms;
using System.IO;
using ProtoDumper.Outputs;

namespace ProtoDumper {
    public class Program {

        public enum ExportType {
            Proto,
            TypeScript
        }

        private const string HelpParam = "--help";
        private const string HelpParamShort = "-h";
        private const string DontDeleteOldProtosParam = "--dont-delete-old-protos";
        private const string AssemblyPathParam = "--assembly-path=";
        private const string OutputPathParam = "--output-path=";
        private const string ExportTypeParam = "--export-type=";
        private const string ExportExtensionParam = "--export-file-extension=";

        [STAThread]
        public static void Main(string[] args) {
            var assemblyPath = "";
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output") + "\\";
            ExportType exportType = ExportType.Proto;
            var exportFileExtension = "";
            var dontDeleteOldProtos = false;

            // If args are empty and assembly path is not defined, popup asking for the assembly
            if (args.Length == 0 && assemblyPath == "") {
                var ofd = new OpenFileDialog();
                ofd.Filter = "Assembly-CSharp.dll|*";
                Console.WriteLine("Please select the Assembly with Proto definitions.");
                if (ofd.ShowDialog() == DialogResult.OK) {
                    assemblyPath = ofd.FileName;
                }
            }
            // Read args
            else if (args.Length > 0) {
                // Inspired on Il2CppAssemblyUnhollower's argument system, thanks knah
                foreach (var arg in args) {
                    if (arg == HelpParam || arg == HelpParamShort) {
                        ShowHelp();
                        return;
                    }
                    else if (arg == DontDeleteOldProtosParam) {
                        dontDeleteOldProtos = true;
                    }
                    else if (arg.StartsWith(AssemblyPathParam)) {
                        assemblyPath = arg.Substring(AssemblyPathParam.Length);
                    }
                    else if (arg.StartsWith(ExportTypeParam)) {
                        var exportTypeParam = arg.Substring(ExportTypeParam.Length).ToLower();
                        switch (exportTypeParam) {
                            case "typescript":
                            case "ts":
                                exportType = ExportType.TypeScript;
                                break;
                            case "proto":
                                exportType = ExportType.Proto;
                                break;
                            default:
                                Console.WriteLine($"Could not find export type ${exportTypeParam}! Valid types: typescript, proto");
                                exportType = ExportType.Proto;
                                break;
                        }
                    }
                    else if (arg.StartsWith(OutputPathParam)) {
                        outputPath = arg.Substring(OutputPathParam.Length);
                    }
                    else if (arg.StartsWith(ExportExtensionParam)) {
                        exportFileExtension = arg.Substring(ExportExtensionParam.Length);
                    }
                    else {
                        Console.WriteLine($"Unrecognized option {arg}, use -h for help.");
                    }
                }
            }

            if (assemblyPath == "") {
                Console.WriteLine("Assembly path not found!");
                ShowHelp();
                Exit();
                return;
            }

            Console.WriteLine("Parsing protos...");
            var protoParser = new ProtoParser(assemblyPath);
            var protos = protoParser.Parse();
            Console.WriteLine("Proto parsing done!");

            var outputDirectory = new DirectoryInfo(outputPath);

            if (outputDirectory.Exists && !dontDeleteOldProtos) {
                Console.WriteLine("Old protos found! Deleting.");
                outputDirectory.Delete(true);
            }

            Console.WriteLine($"Dumping to folder: {outputDirectory.FullName}");

            Directory.CreateDirectory(outputDirectory.FullName);
            // outputDirectory.Create();

            switch (exportType) {
                case ExportType.TypeScript:
                    Console.WriteLine("Using export type \"TypeScript\".");
                    var dumperTs = new TypeScriptDumper(protos, outputDirectory.FullName);
                    dumperTs.Dump(exportFileExtension == "" ? "ts" : exportFileExtension);
                    break;
                case ExportType.Proto:
                    Console.WriteLine("Using export type \"Proto\".");
                    var dumperProto = new BaseDumper(protos, outputDirectory.FullName);
                    dumperProto.Dump(exportFileExtension == "" ? "proto" : exportFileExtension);
                    break;
            }

            Console.WriteLine("Done!");
            Exit();
        }

        // Also based on Unhollower
        private static void ShowHelp() {
            Console.WriteLine("Usage: ProtoDumper [parameters]");
            Console.WriteLine("Possible parameters:");
            Console.WriteLine($"\t{HelpParam}, {HelpParamShort} - Optional. Show this help");
            Console.WriteLine($"\t{DontDeleteOldProtosParam} - Optional. Stop the program from deleting old protos");
            Console.WriteLine($"\t{AssemblyPathParam} - Optional. Path to the assembly that contains protos");
            Console.WriteLine($"\t{OutputPathParam} - Optional. Output path");
            Console.WriteLine($"\t{ExportTypeParam} - Optional. Export type, can be typescript or proto");
            Console.WriteLine($"\t{ExportExtensionParam} - Optional. File extension used in the exported files");
        }
        private static void Exit() {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
