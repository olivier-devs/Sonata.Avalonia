using Sonata.Avalonia;

namespace Sonata.Samples.MasterDetail;

public class ShellViewModel : Screen
{
    public IObservableCollection<EmployeeModel> Employees { get; private set; }

    private EmployeeModel? _selectedEmployee;
    public EmployeeModel? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetAndNotify(ref _selectedEmployee, value);
    }

    public ShellViewModel()
    {
        DisplayName = "Master-Detail";
        Employees = new BindableCollection<EmployeeModel>
        {
            new EmployeeModel { Name = "Fred" },
            new EmployeeModel { Name = "Bob" },
        };
        SelectedEmployee = Employees.First();
    }

    public void AddEmployee() => Employees.Add(new EmployeeModel { Name = "Unnamed" });
    public void RemoveEmployee(EmployeeModel item) => Employees.Remove(item);
}

public class EmployeeModel : PropertyChangedBase
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetAndNotify(ref _name, value);
    }
}
