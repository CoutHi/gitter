***GITTER***

Gitter is a minimal work in progress cli tool that will help you keep your 
programs that you manually compile up to date. 

It goes through directories defined by a config and runs update/compilation/binary file linking operations
in those directories

All the update, compilation and link parameters are customized through the config

****Usage****

The config is defined in 
```~/.config/gitter/config.toml```

An Example Config Looks Like This:

```
[program.yazi]
path = "~/git/yazi"
build = "cargo build --release"
update = "git pull"
export = "~/.local/bin"
binary_path = "~/git/yazi/target/release/yazi"

[program.resvg]
path = "~/git/resvg/"
build = "cargo build --release"
update = "git pull"
export = "~/.local/bin"
binary_path = "~/git/resvg/target/release/resvg"

[program.gitter]
path = "~/dotnet/gitter/"
build = "~/dotnet/gitter/build.sh"
update = "git pull"
export = "~/.local/bin"
binary_path = "~/dotnet/gitter/bin/Release/net8.0/linux-x64/publish/gitter"
```

path defines the path to the folder of the source code
build defines the command to run to compile the code
update defines the command to pull the newest source code
export defines where to put the binary that's created by the config
binary_path defines where the generated binary lives in the source code folder

***Disclaimer***

As I've said it's still very early in development and primitive, all config options need to be defined,
it lacks argument arrays in the config, doesn't recover from errors etc.
But languages that are easy to compile such as Rust, Go etc. work as per my testing.
Complicated things like CMake etc. are untested, for those you could try creating scripts that you point the config to.

****To Be Implemented****
- Multiple Values For A Key In Config
- Pretty Output
- A System To Do The Initial Pull Of The Source Code
- Custom Config File Path Argument
