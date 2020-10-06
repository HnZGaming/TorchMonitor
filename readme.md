You'll have to set up a couple of things before building/deploying this Torch plugin...

* Read `postbuild.bat` and make sure all the exe's are installed in your system. This `bat` file is responsible for packaging & deploying the plugin to the local machine's Torch folder.
* Read the `csproj` file and make sure all the referenced files exist in your system. There are some DLL's referenced from a local copy of a forked repo. You can find them in the HnZ project on GitHub. 