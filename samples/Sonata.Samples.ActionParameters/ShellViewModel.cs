using System.Collections.ObjectModel;
using Sonata.Avalonia;

namespace Sonata.Samples.ActionParameters;

public record Person(string Name, int Age);

public class ShellViewModel : Screen
{
    private string _name = "";
    public string Name
    {
        get => _name;
        set => SetAndNotify(ref _name, value);
    }

    private int _age;
    public int Age
    {
        get => _age;
        set => SetAndNotify(ref _age, value);
    }

    private string _status = "Modifie Name/Age, puis Save — le bouton suit le guard";
    public string Status
    {
        get => _status;
        set => SetAndNotify(ref _status, value);
    }

    public ObservableCollection<Person> People { get; } = new()
    {
        new Person("Alice", 30),
        new Person("Bob", 25),
        new Person("Charlie", 35),
    };

    public ShellViewModel()
    {
        DisplayName = "Action Parameters";
    }

    public void Save(string name, int age)
    {
        Status = $"Saved: {name}, {age} years old";
    }

    public bool CanSave(string name, int age)
    {
        return !string.IsNullOrWhiteSpace(name) && age >= 18;
    }

    public void Delete(Person person)
    {
        People.Remove(person);
        Status = $"Deleted: {person.Name}";
    }

    public void Seed(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            People.Add(new Person($"Person {People.Count + 1}", 20 + i));
        }
        Status = $"Seeded {count} people";
    }

    public void Clear()
    {
        People.Clear();
        Status = "Cleared all people";
    }
}
