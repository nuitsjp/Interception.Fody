using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Weaving;
using Weaving.Fody;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
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
        var originalName = addMethod.Name;
        addMethod.Name = "AddInner";
        addMethod.Attributes &= ~MethodAttributes.Public;
        addMethod.Attributes |= MethodAttributes.Private;
        //addInnerMethod.Name = "Add";


        var innerInvoker = InnerInvoker.Create(ModuleDefinition, type, addMethod);
        var targetMethod = CreateAddMethod(type, addMethod, innerInvoker, originalName);

        type.Methods.Add(targetMethod);
        type.NestedTypes.Add(innerInvoker.TypeDefinition);
        CreateGetInterceptAttribute(type, addMethod, innerInvoker);

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

    private void CreateGetInterceptAttribute(TypeDefinition type, MethodDefinition originalMethod, InnerInvoker innerInvoker)
    {
        var targetMethod = type.Methods.Single(x => x.Name == "GetInterceptAttribute");
        targetMethod.Body.Instructions.Clear();

        // MethodInfo methodInfo = type.GetMethod("Add2Inner");
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, originalMethod));
        var getMethodFromHandle =
            typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getMethodFromHandle)));

        // InterceptAttribute interceptorAttribute = ((MemberInfo)methodInfo).GetCustomAttribute<InterceptAttribute>();
        var getCustomAttribute = typeof(CustomAttributeExtensions).GetMethods()
            .Where(x => x.Name == "GetCustomAttribute" && x.GetGenericArguments().Length == 1)
            .Single(x =>
            {
                var parameters = x.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(MemberInfo);
            }).MakeGenericMethod(typeof(InterceptAttribute));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getCustomAttribute)));

        // interceptorAttribute.InterceptorTypes
        var get_InterceptorTypes = typeof(InterceptAttribute).GetTypeInfo().GetMethod("get_InterceptorTypes");
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(get_InterceptorTypes)));

        // new AddInvocation
        var innerInvokerConstructor = innerInvoker.TypeDefinition.GetConstructors().Single();
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, innerInvokerConstructor));
        // AddInvocation.Class = this
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, innerInvoker.ParentTypeFieldDefinition));
        //  AddInvocation.ValueN = ParamN
        for (var i = 0; i < innerInvoker.ParameterFieldDefinisions.Count; i++)
        {
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, targetMethod.Parameters[i]));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, innerInvoker.ParameterFieldDefinisions[i]));
        }
        // invocation.Invoke();
        var invoke = typeof(IInvocation).GetTypeInfo().DeclaredMethods.Single(x => x.Name == "Invoke");
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ModuleDefinition.ImportReference(invoke)));

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    private MethodDefinition CreateAddMethod(TypeDefinition type, MethodDefinition originalMethod, InnerInvoker innerInvoker, string originalName)
    {
        var targetMethod =
            new MethodDefinition(originalName, MethodAttributes.Public | MethodAttributes.HideBySig, type)
            {
                Body =
                {
                    MaxStackSize = originalMethod.Parameters.Count + 1,
                    InitLocals = true
                },
                ReturnType = originalMethod.ReturnType
            };
        // Add Parameter
        foreach (var parameter in originalMethod.Parameters)
        {
            var newParameter = new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType);
            targetMethod.Parameters.Add(newParameter);
        }

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, originalMethod));
        var getMethodFromHandle =
            typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getMethodFromHandle)));

        // InterceptAttribute interceptorAttribute = ((MemberInfo)methodInfo).GetCustomAttribute<InterceptAttribute>();
        var getCustomAttribute = typeof(CustomAttributeExtensions).GetMethods()
            .Where(x => x.Name == "GetCustomAttribute" && x.GetGenericArguments().Length == 1)
            .Single(x =>
            {
                var parameters = x.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(MemberInfo);
            }).MakeGenericMethod(typeof(InterceptAttribute));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getCustomAttribute)));

        // interceptorAttribute.InterceptorTypes
        var get_InterceptorTypes = typeof(InterceptAttribute).GetTypeInfo().GetMethod("get_InterceptorTypes");
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(get_InterceptorTypes)));

        // new AddInvocation
        var innerInvokerConstructor = innerInvoker.TypeDefinition.GetConstructors().Single();
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, innerInvokerConstructor));
        // AddInvocation.Class = this
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, innerInvoker.ParentTypeFieldDefinition));
        //  AddInvocation.ValueN = ParamN
        for (var i = 0; i < innerInvoker.ParameterFieldDefinisions.Count; i++)
        {
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, targetMethod.Parameters[i]));
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, innerInvoker.ParameterFieldDefinisions[i]));
        }
        // invocation.Invoke();
        var invoke = typeof(IInvocation).GetTypeInfo().DeclaredMethods.Single(x => x.Name == "Invoke");
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ModuleDefinition.ImportReference(invoke)));

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return targetMethod;
    }
}
