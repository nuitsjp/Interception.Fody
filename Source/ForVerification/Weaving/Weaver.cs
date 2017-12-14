using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Weaving
{
    public class Weaver
    {
        private MethodInfo DebugWriteLine { get; } =
            typeof(System.Diagnostics.Debug)
                .GetTypeInfo()
                .DeclaredMethods
                .Where(x => x.Name == nameof(System.Diagnostics.Debug.WriteLine))
                .Single(x =>
                {
                    var parameters = x.GetParameters();
                    return parameters.Length == 1 &&
                           parameters[0].ParameterType == typeof(string);
                });

        public void Weave(ModuleDefinition module)
        {
            var type = module.Types.Single(x => x.Name == "Class1");

            var addMethod = type.Methods.Single(x => x.Name == "Add");
            //var addInnerMethod = type.Methods.Single(x => x.Name == "AddInner");
            addMethod.Name = "AddInner";
            //addInnerMethod.Name = "Add";

            var targetMethod =
                new MethodDefinition("Add", MethodAttributes.Public | MethodAttributes.HideBySig, type)
                {
                    Body =
                    {
                        MaxStackSize = addMethod.Parameters.Count + 1,
                        InitLocals = true
                    },
                    ReturnType = addMethod.ReturnType
                };

            targetMethod.Body.Variables.Add(new VariableDefinition(addMethod.ReturnType));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, $"DEBUG LOG: {addMethod.DeclaringType.Name}#{addMethod.Name}()"));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(DebugWriteLine)));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            addMethod.Parameters.ToList().ForEach(x =>
            {
                targetMethod.Parameters.Add(x);
                targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, x));
            });
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, addMethod));

            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            type.Methods.Add(targetMethod);


            //var methods = module
            //    .Types
            //    .SelectMany(x => x.Methods);
            //foreach (var method in methods)
            //{
            //    var processor = method.Body.GetILProcessor();
            //    var current = method.Body.Instructions.First();

            //    processor.InsertBefore(current, Instruction.Create(OpCodes.Nop));
            //    processor.InsertBefore(current, Instruction.Create(OpCodes.Ldstr, $"DEBUG LOG: {method.DeclaringType.Name}#{method.Name}()"));
            //    processor.InsertBefore(current, Instruction.Create(OpCodes.Call, module.ImportReference(DebugWriteLine)));
            //}
        }
    }
}
