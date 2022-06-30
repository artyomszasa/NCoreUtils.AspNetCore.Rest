using System.Collections.Generic;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Unit.Data;

public class TestData : IHasId<int>
{
    public int Id { get; }

    public string StringData { get; }

    public double DoubleData { get; }

    public IReadOnlyList<string> StringsData { get; }

    public TestData(int id, string stringData, double doubleData, IReadOnlyList<string> stringsData)
    {
        Id = id;
        StringData = stringData;
        DoubleData = doubleData;
        StringsData = stringsData;
    }
}