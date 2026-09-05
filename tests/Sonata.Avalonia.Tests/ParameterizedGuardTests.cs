using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;
using Sonata.Avalonia.Internal;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ParameterizedGuardTests
{
    public class Target : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanSave(string name, int age) => !string.IsNullOrWhiteSpace(name) && age >= 18;

        public void Save(string name, int age) { }
    }

    [Fact]
    public void CanExecute_UsesParameterizedGuard()
    {
        var target = new Target();
        var name = new Parameter { Value = "" };
        var age = new Parameter { Value = 20 };
        var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { name, age });

        Assert.False(action.CanExecute(null));

        name.Value = "Alice";
        Assert.True(action.CanExecute(null));
    }

    // C1 — Guard Can* retournant Task → ActionSignatureInvalidException
    public class GuardTaskTarget
    {
        public Task CanSave(string name) => Task.CompletedTask;
        public void Save(string name) { }
    }

    [Fact]
    public void CanExecute_GuardReturningTask_ThrowsActionSignatureInvalidException()
    {
        var target = new GuardTaskTarget();
        var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { new Parameter { Value = "x" } });

        var ex = Assert.Throws<ActionSignatureInvalidException>(() => action.CanExecute(null));
        Assert.Contains("CanSave", ex.Message);
        Assert.Contains("must return bool", ex.Message);
    }

    // C2 — Guard méthode uniforme sur le chemin CommandParameter (1 argument)
    public class CommandParameterGuardTarget
    {
        public void Delete(int id) { }
        public bool CanDelete(object value) => value is int n && n > 0;
    }

    [Fact]
    public void CanExecute_UsesMethodGuardForSingleCommandParameter()
    {
        var target = new CommandParameterGuardTarget();
        var action = new CommandAction(target, "Delete", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw);

        Assert.True(action.CanExecute(42));
        Assert.False(action.CanExecute(0));
        Assert.False(action.CanExecute(null));
    }

    // C5 — Warning « guard méthode + propriété tous deux présents »
    private static object CreateMixedGuardTarget()
    {
        var assemblyName = new AssemblyName("DynamicMixedGuard");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        var typeBuilder = moduleBuilder.DefineType("MixedGuardTarget", TypeAttributes.Public);

        // bool CanSave { get; set; }
        var backingField = typeBuilder.DefineField("_canSave", typeof(bool), FieldAttributes.Private);
        var propBuilder = typeBuilder.DefineProperty("CanSave", PropertyAttributes.None, typeof(bool), null);

        var getter = typeBuilder.DefineMethod("get_CanSave",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(bool), Type.EmptyTypes);
        var getIl = getter.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, backingField);
        getIl.Emit(OpCodes.Ret);

        var setter = typeBuilder.DefineMethod("set_CanSave",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, new[] { typeof(bool) });
        var setIl = setter.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, backingField);
        setIl.Emit(OpCodes.Ret);

        propBuilder.SetGetMethod(getter);
        propBuilder.SetSetMethod(setter);

        // bool CanSave(string name)
        var guardMethod = typeBuilder.DefineMethod("CanSave",
            MethodAttributes.Public,
            typeof(bool), new[] { typeof(string) });
        var guardIl = guardMethod.GetILGenerator();
        guardIl.Emit(OpCodes.Ldc_I4_1);
        guardIl.Emit(OpCodes.Ret);

        // void Save(string name)
        var saveMethod = typeBuilder.DefineMethod("Save",
            MethodAttributes.Public,
            null, new[] { typeof(string) });
        var saveIl = saveMethod.GetILGenerator();
        saveIl.Emit(OpCodes.Ret);

        var createdType = typeBuilder.CreateType();
        return Activator.CreateInstance(createdType)!;
    }

    [Fact]
    public void CanExecute_MixedGuardMethodAndProperty_LogsWarning()
    {
        var provider = new TestLoggerProvider();
        var factory = new LoggerFactory(new[] { provider });
        SonataLogManager.SetFactory(factory);
        try
        {
            var target = CreateMixedGuardTarget();
            var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
                new ActionParameter[] { new Parameter { Value = "x" } });

            action.CanExecute(null);

            Assert.Contains(provider.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("guard method"));
        }
        finally
        {
            SonataLogManager.SetFactory(null);
        }
    }
}
