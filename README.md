# SharpServer

C# server implementing the protocol described in [ProtoSpecs](https://github.com/CroissantBit/ProtoSpecs)

## Usage

Common flags:

- `-h` or `--help`: Display help message
- `-v` or `--verbose`: Display verbose output
- ...

```bash
# TODO: Add CLI usage instructions
```

## Building

Before doing anything, make sure you have the following installed:

- Git
- [.Net 6.0](https://learn.microsoft.com/en-us/dotnet/core/install/) (Should come preinstalled with your IDE)
- [DotNet CLI](https://learn.microsoft.com/en-us/dotnet/core/install/) (Should come included with the .Net SDK)

After installing the required dependencies, clone the repository and pull the required git submodules:

```bash
# Clone the repository
git clone https://github.com/CroissantBit/SharpServer
cd SharpServer

# Pull the required git submodules
git submodule update --init --recursive
```

Continue with the instructions for your preferred build method:

### IDE

Visual Studio or Rider should be able to build the project without too much hassle using the MSBuild configuration.
See `SharpServer.csproj` for the build steps.

### CLI

```bash
# Make sure dotnet CLI is installed
dotnet --version

# Install the required NuGet packages
dotnet restore

# Build the Protocol Buffers for C#
# Either use the included protoc compiler from 'google.protobuf.tools' package or install protoc manually
# When installing protoc manually, make sure to check that the version is up to date (use the same version that the project uses for best compatibility)
protoc -I proto/src --csharp_out=SharpServer/Generated proto/src/*.proto

# TODO: Finish instructions for compiling with dotnet CLI for Win and Linux targets
```
