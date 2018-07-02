dotnet restore

dotnet build -c "Release"

dotnet pack ".\src\Noobot.Toolbox\Noobot.Toolbox.csproj" --no-build --no-restore --output "..\..\nupkgs" -c "Release"
