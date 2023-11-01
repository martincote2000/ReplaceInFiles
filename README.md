# ReplaceInFiles
Consol


# How to use it

## Examples


**Replace value with match pattern `${...}`**
```command
"ReplaceInFiles.exe" ^
	--folder "C:\MyFolder" ^
	--extensions "js,html" ^	
	--ignorefoldernames ".git,.vs,packages,jquery,kendo,limitless,plugins,node_modules" ^
	--replaceparameters "${HostName}/api/MySpecialApi=https://dev.mysystem.com" ^
	--includeSubFolder true 
```

**Replace value with multiple parameters**
```command
"ReplaceInFiles.exe" ^
	--folder "C:\MyFolder" ^
	--extensions "js,html" ^
	--parallelexecution 10 ^
	--ignorefoldernames ".git,.vs,packages,jquery,kendo,limitless,plugins,node_modules" ^
	--replaceparameters "${HostName}=https://dev.mysystem.com;${HostPort}=8888" ^
	--includeSubFolder true 
```

**Replace value without match pattern**
The file replacer will search the exact string in the files found.
```command
"ReplaceInFiles.exe" ^
	--folder "C:\MyFolder" ^
	--extensions "js,html" ^	
	--ignorefoldernames ".git,.vs,packages,jquery,kendo,limitless,plugins,node_modules" ^
	--nopattern true ^
	--replaceparameters "http://localhost/api/MySpecialApi=${HostName}/api/MySpecialApi;" ^
	--includeSubFolder true 
```


## parameters

```
Replace values or parameter ${} in files.

-f|--folder              Folder to search files Examples: C:\MyFolder
-e|--extensions          A list of file extensions. Multiple values can be used
                         by joining them with separators ";"
						 Example "js,html,json"

--ignorefoldernames      A list of folder names to ignore (ex: bin, obj, .git).
                         Multiple values can be used by joining them with any
                         of the following separators: ;
						 
-p|--replaceparameters   List of parameter to replace in files. Parameter in
                         the file is ${...variable name...}. Examples:
                         --replaceparameters
                         "ParameterName1=MyValue1;ParameterName2=MyValue2;"Multi
                         ple values can be used by joining them with any of the
                         following separators: ;
						 
-rep|--parallelexecution Optional with default '5'. Number of simultaneous parallels replacement

--verbose                Optional. Verbose mode

-nopattern|--nopattern   Optional. No search pattern. Usefull to find raw value.

-sf|--includesubfolder   Optional with default 'True'. Include sub folder in
                         the search
						 
```




