Generates `src/TurtleRoute.OpenCage/AddressFormattingData.g.cs`: the street abbreviations and country languages of
[OpenCageData/address-formatting](https://github.com/OpenCageData/address-formatting) (MIT), plus `supplement.yaml` for
abbreviations missing upstream.

```
dotnet run tools/address-formatting/Generate.cs           # regenerate from the commit in the current file
dotnet run tools/address-formatting/Generate.cs master    # move to the latest upstream commit
```

Commit the regenerated file; its diff shows what changed upstream.
