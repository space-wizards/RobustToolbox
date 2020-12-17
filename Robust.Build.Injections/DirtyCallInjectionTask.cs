using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Robust.Build.Injections
{
    public partial class DirtyCallInjectionTask : ITask
    {
        [Required]
        public string AssemblyFile { get; set; }

        [Required]
        public string IntermediatePath { get; set; }

        [Required]
        public string AssemblyReferencePath { get; set; }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            var originalCopyPath = $"{IntermediatePath}dirty_call_injector_copy.dll";
            var pdbExists = false;
            try
            {
                File.Copy(AssemblyFile, originalCopyPath, true);
                File.Delete(AssemblyFile);

                var inputPdb = GetPdbPath(AssemblyFile);
                if (File.Exists(inputPdb))
                {
                    var copyPdb = GetPdbPath(originalCopyPath);
                    File.Copy(inputPdb, copyPdb, true);
                    File.Delete(inputPdb);
                    pdbExists = true;
                }
            }
            catch (Exception e)
            {
                BuildEngine.LogError("AssemblyLoading",$"Error while copying Assembly: {e}!", "");
                return false;
            }

            BuildEngine.LogMessage($"DirtyCallInjection -> AssemblyFile:{AssemblyFile}", MessageImportance.Low);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(AssemblyReferencePath);
            var readerParameters = new ReaderParameters { AssemblyResolver = resolver };
            if (pdbExists)
            {
                readerParameters.ReadSymbols = true;
            }

            var asdef = AssemblyDefinition.ReadAssembly(originalCopyPath, readerParameters);

            try
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            var iCompType = asdef.MainModule.GetType("Robust.Shared.Interfaces.GameObjects.IComponent");
            if(iCompType == null)
            {
                if (!asdef.MainModule.TryGetTypeReference("Robust.Shared.Interfaces.GameObjects.IComponent",
                    out var iCompTypeRef))
                {
                    BuildEngine.LogError("DirtyMethodFinder","No IComponent-Type found!", "");
                    return false;
                }
                else
                {
                    iCompType = iCompTypeRef.Resolve();
                }
            }

            var dirtyMethod = iCompType.Methods.FirstOrDefault(m => m.Name == "Dirty");
            if (dirtyMethod == null)
            {
                BuildEngine.LogError("DirtyMethodFinder","No Dirty-Method found!", "");
                return false;
            }

            var internalDirtyMethod = asdef.MainModule.ImportReference(dirtyMethod);

            foreach (var typeDef in asdef.MainModule.Types)
            {
                if(!IsComponent(typeDef)) continue;

                foreach (var propDef in typeDef.Properties.Where(propDef => propDef.CustomAttributes.Any(a => a.AttributeType.FullName == "Robust.Shared.Injections.DirtyAttribute")))
                {
                    BuildEngine.LogMessage($"Found marked property {propDef} of type {typeDef}.", MessageImportance.Low);

                    if (!IsDefaultBody(propDef.SetMethod.Body))
                    {
                        BuildEngine.LogError("CustomSetterFound",$"Property {propDef} of type {typeDef} was marked [Dirty] but has a custom Setter.", typeDef.FullName);
                        return false;
                    }

                    bool shouldDoCheck;
                    try
                    {
                        shouldDoCheck = (bool) propDef.CustomAttributes.First(a =>
                                a.AttributeType.FullName == "Robust.Shared.Injections.DirtyAttribute")
                            .ConstructorArguments
                            .First().Value;
                    }
                    catch (Exception e)
                    {
                        BuildEngine.LogError("OnlyOnNewValueGetter", $"Error while getting OnlyOnNewValue-Value: {e}", propDef.FullName);
                        return false;
                    }

                    var ilProcessor = propDef.SetMethod.Body.GetILProcessor();
                    var instr = ilProcessor.Body.Instructions;
                    var setterThis = instr[0];
                    var setterValue = instr[1];
                    var setterStoreInField = instr[2];
                    var setterReturn = instr[3];
                    ilProcessor.Clear();
                    if (shouldDoCheck)
                    {
                        var equalsMethod = GetEqualsMethodRecursive(asdef.MainModule, propDef.PropertyType.Resolve());
                        if (equalsMethod == null)
                        {
                            BuildEngine.LogWarning("EqualsMethodGetter", $"Failed trying to get Equals-Method for type {propDef.PropertyType}, skipping Equals-Injection.", propDef.FullName);
                        }
                        else
                        {
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_1));
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldfld, (FieldReference)setterStoreInField.Operand));
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, equalsMethod));
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Brfalse, setterReturn));
                        }
                    }
                    ilProcessor.Append(setterThis);
                    ilProcessor.Append(setterValue);
                    ilProcessor.Append(setterStoreInField);
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Call, internalDirtyMethod));
                    ilProcessor.Append(setterReturn);
                }
            }

            if (pdbExists)
            {
                var writerParameters = new WriterParameters {WriteSymbols = true};
                asdef.Write(AssemblyFile, writerParameters);
            }else
            {
                asdef.Write(AssemblyFile);
            }

            resolver.Dispose();
            asdef.Dispose();
            return true;
        }
    }
}
