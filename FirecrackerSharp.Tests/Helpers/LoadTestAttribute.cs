namespace FirecrackerSharp.Tests.Helpers;

public sealed class LoadTestAttribute : TheoryAttribute
{
    public LoadTestAttribute(int minAmount)
    {
        var variable = Environment.GetEnvironmentVariable("FSH_LOAD_VM_AMOUNT");
        if (variable is null)
        {
            Skip = "Load VM amount hasn't been set";
            return;
        }

        var vmAmount = Convert.ToInt32(variable);
        if (vmAmount < minAmount)
        {
            Skip = $"This test requires consent to {minAmount} VMs, but only {vmAmount} VMs are consented to";
        }
    }
}