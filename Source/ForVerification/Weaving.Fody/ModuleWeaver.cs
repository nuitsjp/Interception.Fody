using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Weaving;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

public class ModuleWeaver
{
    public ModuleDefinition ModuleDefinition { get; set; }

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

    //private MethodInfo GetCustomAttribute { get; } =
    //    typeof(Type)
    //        .GetTypeInfo().GetMethod("GetCustomAttribute", new [] {typeof(InterceptAttribute)});

    public void Execute()
    {
        var type = ModuleDefinition.Types.Single(x => x.Name == "Class1");

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

        targetMethod.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(typeof(MethodInfo))));
        targetMethod.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(typeof(InterceptAttribute))));
        targetMethod.Body.Variables.Add(new VariableDefinition(addMethod.ReturnType));

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, addMethod));
        var getMethodFromHandle =
            typeof(MethodBase).GetMethod("GetMethodFromHandle", new [] {typeof(RuntimeMethodHandle)});
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getMethodFromHandle)));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        var getCustomAttribute = typeof(CustomAttributeExtensions).GetMethods()
            .Where(x => x.Name == "GetCustomAttribute" && x.GetGenericArguments().Length == 1)
            .Single(x =>
            {
                var parameters = x.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(MemberInfo);
            });
        var genericGetCustomAttribute = getCustomAttribute.MakeGenericMethod(typeof(InterceptAttribute));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(genericGetCustomAttribute)));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));


        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, $"DEBUG LOG: {addMethod.DeclaringType.Name}#{addMethod.Name}()"));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(DebugWriteLine)));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        addMethod.Parameters.ToList().ForEach(x =>
        {
            targetMethod.Parameters.Add(x);
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, x));
        });
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, addMethod));

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        type.Methods.Add(targetMethod);

        type.NestedTypes.Add(CreateInnerInvoker(type));

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

    private TypeDefinition CreateInnerInvoker(TypeDefinition parent)
    {
        var innerInvoker = new TypeDefinition(parent.Namespace, "AddInnerInvoker", TypeAttributes.NotPublic);
        return innerInvoker;
    }
}
