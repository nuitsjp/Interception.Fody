using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Weaving;
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
        addMethod.Name = "AddInner";
        addMethod.Attributes &= ~MethodAttributes.Public;
        addMethod.Attributes |= MethodAttributes.Private;
        //addInnerMethod.Name = "Add";

        var targetMethod = CreateAddMethod(type, addMethod);

        type.Methods.Add(targetMethod);

        var innerInvoker = CreateInnerInvoker(type, addMethod);
        type.NestedTypes.Add(innerInvoker);
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

    private void CreateGetInterceptAttribute(TypeDefinition type, MethodDefinition originalMethod, TypeDefinition innerInvoker)
    {
        var method = type.Methods.Single(x => x.Name == "GetInterceptAttribute");
        method.Body.Instructions.Clear();

        // MethodInfo methodInfo = type.GetMethod("Add2Inner");
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, originalMethod));
        var getMethodFromHandle =
            typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getMethodFromHandle)));

        // InterceptAttribute interceptorAttribute = ((MemberInfo)methodInfo).GetCustomAttribute<InterceptAttribute>();
        var getCustomAttribute = typeof(CustomAttributeExtensions).GetMethods()
            .Where(x => x.Name == "GetCustomAttribute" && x.GetGenericArguments().Length == 1)
            .Single(x =>
            {
                var parameters = x.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(MemberInfo);
            }).MakeGenericMethod(typeof(InterceptAttribute));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getCustomAttribute)));

        // interceptorAttribute.InterceptorTypes
        var get_InterceptorTypes = typeof(InterceptAttribute).GetTypeInfo().GetMethod("get_InterceptorTypes");
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(get_InterceptorTypes)));

        // new AddInvocation
        var innerInvokerConstructor = innerInvoker.GetConstructors().Single();
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, innerInvokerConstructor));
        // AddInvocation.Class = this
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        ////  AddInvocation.ValueN = ParamN
        //for (int i = 1; i <= originalMethod.Parameters.Count; i++)
        //{
        //    method.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
        //    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, i));
        //}
        // invocation.Invoke();
        //var invoke = typeof(IInvocation).GetTypeInfo().DeclaredMethods.Single(x => x.Name == "Invoke");
        //method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ModuleDefinition.ImportReference(invoke)));


        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));




        //method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, originalMethod));
        //var getMethodFromHandle =
        //    typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        //method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(getMethodFromHandle)));
        //method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

    }

    private MethodDefinition CreateAddMethod(TypeDefinition type, MethodDefinition originalMethod)
    {
        var targetMethod =
            new MethodDefinition("Add", MethodAttributes.Public | MethodAttributes.HideBySig, type)
            {
                Body =
                {
                    MaxStackSize = originalMethod.Parameters.Count + 1,
                    InitLocals = true
                },
                ReturnType = originalMethod.ReturnType
            };

        // ローカル変数定義
        targetMethod.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(typeof(MethodInfo))));
        targetMethod.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(typeof(InterceptAttribute))));
        // 戻り値型定義
        targetMethod.Body.Variables.Add(new VariableDefinition(originalMethod.ReturnType));

        // 
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, originalMethod));
        var getMethodFromHandle =
            typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
            ModuleDefinition.ImportReference(getMethodFromHandle)));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

        // InterceptAttribute customAttribute = ((MemberInfo)methodFromHandle).GetCustomAttribute<InterceptAttribute>();
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        var getCustomAttribute = typeof(CustomAttributeExtensions).GetMethods()
            .Where(x => x.Name == "GetCustomAttribute" && x.GetGenericArguments().Length == 1)
            .Single(x =>
            {
                var parameters = x.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(MemberInfo);
            });
        var genericGetCustomAttribute = getCustomAttribute.MakeGenericMethod(typeof(InterceptAttribute));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
            ModuleDefinition.ImportReference(genericGetCustomAttribute)));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));


        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr,
            $"DEBUG LOG: {originalMethod.DeclaringType.Name}#{originalMethod.Name}()"));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
            ModuleDefinition.ImportReference(DebugWriteLine)));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        originalMethod.Parameters.ToList().ForEach(x =>
        {
            targetMethod.Parameters.Add(x);
            targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, x));
        });
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, originalMethod));

        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
        targetMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return targetMethod;
    }

    private TypeDefinition CreateInnerInvoker(TypeDefinition parent, MethodDefinition targetMethod)
    {
        var invocationType = ModuleDefinition.ImportReference(typeof(Invocation));
        

        var innerInvoker = new TypeDefinition(parent.Namespace, "AddInnerInvoker", TypeAttributes.NotPublic | TypeAttributes.NestedPrivate);
        innerInvoker.BaseType = invocationType;


        // Constructor
        var methodAttributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                               MethodAttributes.RTSpecialName;
        var constructor = new MethodDefinition(".ctor", methodAttributes, ModuleDefinition.TypeSystem.Void);
        constructor.Parameters.Add(new ParameterDefinition("interceptorTypes", ParameterAttributes.None, ModuleDefinition.ImportReference(typeof(Type).MakeArrayType())));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));

        var baseConstructor = invocationType.Resolve().GetConstructors().Single();
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(baseConstructor)));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        // Fields
        var parentField = new FieldDefinition(parent.Name, FieldAttributes.Assembly, parent);
        innerInvoker.Fields.Add(parentField);

        var valueFieldDefinitions = new List<FieldDefinition>();
        foreach (var (parameter, index) in targetMethod.Parameters.Select((parameter, index) => (parameter, index)))
        {
            var valueFieldDefinition =
                new FieldDefinition($"Value{index + 1}", FieldAttributes.Assembly, parameter.ParameterType);
            valueFieldDefinitions.Add(valueFieldDefinition);
            innerInvoker.Fields.Add(valueFieldDefinition);
        }
        
        // Properties
        var arguments = new PropertyDefinition("Arguments", PropertyAttributes.None, ModuleDefinition.ImportReference(typeof(object).MakeArrayType())){HasThis = true};

        var getArgumentsAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
            MethodAttributes.Virtual;
        var getArguments = new MethodDefinition("get_Arguments", getArgumentsAttributes, ModuleDefinition.ImportReference(typeof(object).MakeArrayType())){HasThis = true};
        getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, targetMethod.Parameters.Count));
        getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object));
        getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));

        foreach (var (fieldDefinition, index) in valueFieldDefinitions.Select((fieldDefinition, index) => (fieldDefinition, index)))
        {
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, index));
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Box, ModuleDefinition.TypeSystem.Int32));
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }

        getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        innerInvoker.Methods.Add(getArguments);

        arguments.GetMethod = getArguments;
        innerInvoker.Properties.Add(arguments);

        // InvokeEndpoint
        var invokeEndpointAttribute = MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual;
        var invokeEndpoint = new MethodDefinition("InvokeEndpoint", invokeEndpointAttribute, ModuleDefinition.TypeSystem.Object);
        invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, parentField));
        foreach (var valueFieldDefinition in valueFieldDefinitions)
        {
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, valueFieldDefinition));
        }
        invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, targetMethod));
        invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Box, ModuleDefinition.TypeSystem.Int32));
        invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        innerInvoker.Methods.Add(invokeEndpoint);


        innerInvoker.Methods.Add(constructor);
        return innerInvoker;
    }
}
