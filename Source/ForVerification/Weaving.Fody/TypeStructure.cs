using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Weaving.Fody
{
    public class TypeStructure
    {
        private ModuleDefinition ModuleDefinition { get; }
        public IList<PropertyDefinition> VariableReactiveProperties { get; } = new List<PropertyDefinition>();
        public IList<PropertyDefinition> ConstantReactiveProperties { get; } = new List<PropertyDefinition>();
        public IList<PropertyDefinition> VariableReactiveCommands { get; } = new List<PropertyDefinition>();
        public IList<PropertyDefinition> ConstantReactiveCommands { get; } = new List<PropertyDefinition>();
        public IList<PropertyDefinition> VariableAsyncReactiveCommands { get; } = new List<PropertyDefinition>();
        public IList<PropertyDefinition> ConstantAsyncReactiveCommands { get; } = new List<PropertyDefinition>();
        public TypeStructure(ModuleDefinition moduleDefinition, TypeDefinition typeDefinition)
        {
            ModuleDefinition = moduleDefinition;
            foreach (var propertyDefinition in typeDefinition.Properties)
            {
                var propertyType = GetPropertyType(propertyDefinition);
                switch (propertyType)
                {
                    case PropertyType.IReactiveProperty:
                        Add(propertyDefinition, VariableReactiveProperties, ConstantReactiveProperties);
                        break;
                    case PropertyType.IReadOnlyReactiveProperty:
                        Add(propertyDefinition, VariableReactiveProperties, ConstantReactiveProperties);
                        break;
                    case PropertyType.ReactiveCommand:
                        Add(propertyDefinition, VariableReactiveCommands, ConstantReactiveCommands);
                        break;
                    case PropertyType.AsyncReactiveCommand:
                        Add(propertyDefinition, VariableAsyncReactiveCommands, ConstantAsyncReactiveCommands);
                        break;
                    case PropertyType.Other:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void Add(PropertyDefinition propertyDefinition, IList<PropertyDefinition> valiableList,
            IList<PropertyDefinition> constantList)
        {
            if (propertyDefinition.SetMethod != null)
            {
                valiableList.Add(propertyDefinition);
            }
            else
            {
                constantList.Add(propertyDefinition);
            }
        }

        private PropertyType GetPropertyType(PropertyDefinition propertyDefinition)
        {
            var propertyType = propertyDefinition.PropertyType.Resolve();
            if (propertyType.Interfaces.Any(
                x => x.InterfaceType.FullName == "Reactive.Bindings.IReactiveProperty"))
            {
                return PropertyType.IReactiveProperty;
            }

            if (propertyType.Interfaces.Any(
                x => x.InterfaceType.FullName == "Reactive.Bindings.IReadOnlyReactiveProperty"))
            {
                return PropertyType.IReadOnlyReactiveProperty;
            }
            if (IsReactiveCommand(propertyDefinition))
            {
                return PropertyType.ReactiveCommand;
            }
            if (IsAsyncReactiveCommand(propertyDefinition))
            {
                return PropertyType.AsyncReactiveCommand;
            }
            return PropertyType.Other;
        }

        private bool IsReactiveCommand(PropertyDefinition propertyDefinition)
        {
            for (var propertyType = propertyDefinition.PropertyType; 
                propertyType != null; 
                propertyType = propertyType.Resolve().BaseType)
            {
                if (propertyType.FullName.StartsWith("Reactive.Bindings.ReactiveCommand"))
                    return true;
            }
            return false;
        }

        private bool IsAsyncReactiveCommand(PropertyDefinition propertyDefinition)
        {
            for (var propertyType = propertyDefinition.PropertyType;
                propertyType != null;
                propertyType = propertyType.Resolve().BaseType)
            {
                if (propertyType.FullName.StartsWith("Reactive.Bindings.AsyncReactiveCommand"))
                    return true;
            }
            return false;
        }

    }
}
