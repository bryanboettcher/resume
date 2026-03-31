FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ResumeChat.slnx .
COPY src/ResumeChat.Api/ResumeChat.Api.csproj src/ResumeChat.Api/
COPY src/ResumeChat.Rag/ResumeChat.Rag.csproj src/ResumeChat.Rag/
COPY tests/ResumeChat.Api.Tests/ResumeChat.Api.Tests.csproj tests/ResumeChat.Api.Tests/
COPY tests/ResumeChat.Rag.Tests/ResumeChat.Rag.Tests.csproj tests/ResumeChat.Rag.Tests/
RUN dotnet restore

COPY . .
RUN dotnet test --no-restore --verbosity quiet
RUN dotnet publish src/ResumeChat.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "ResumeChat.Api.dll"]
