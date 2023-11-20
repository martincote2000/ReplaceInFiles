# OpsUtil.FileOperations  
The main objective of this tool to help DevOps operations with files which are not structured to be replaced 
with a integrated variable substitution.

Libraries that helps with multiple FileOperation such as finding Files and replaces values. 
The library has the capability to :
- find specific file extensions
- search in folders and/or sub-folders
- exclude folders (ex: .vs, .git, packages, node_modules)
- ignore character casing
- Replace value using using match pattern or exact match
  - match pattern `${}`  
  - exact string (without match pattern)
- Replace multiples files in parallel

The files supported are any type that can be read in text format.   
**Example:** txt, json, html

The variable to find using
- match pattern `${}`  
- exact string (without match pattern)

The library is build to be used with in a Console application or as a library.


## How to use it

### Find and replace using match pattern & multiple parameters

You could add multiple parameters seperated by semi-colon.  
`--parameters "${FirstParam}=Value1;${SecondParam}=Value2"`


```command
"OpsUtil.ReplaceInFiles.Cli.exe" ^
	--folder "C:\App" ^
	--extensions "js" ^	
	--ignorefoldernames ".git,.vs,packages,node_modules" ^
	--parameters "${HostName}=https://dev.mysystem.com;${HostPort}=8888" ^
	--includeSubFolder true 
```

**Javascript file example app.js**
```javascript
(function () {
    'use strict';
    angular
        .module('MyApp')
        .constant("API_HOSTS", (function () {
            return {
                URL: {
                    ROOT_PATH: '${RootPath}:${HostPort}/api/MySpecialApi',
                }
            }
        })());
})();
```


### Find and replace exact string
The file replacer will search the exact string in the files found using `nopattern` parameter.


```command
"OpsUtil.ReplaceInFiles.Cli.exe" ^
	--folder "C:\App" ^
	--extensions "js,html" ^	
	--ignorefoldernames ".git,.vs,packages,node_modules" ^
	--nopattern true ^
	--parameters "http://localhost/api/MySpecialApi=${HostName}/api/MySpecialApi;" ^
	--includeSubFolder true 
```


**javascript file example app.js**
```javascript
(function () {
    'use strict';
    angular
        .module('MyApp')
        .constant("API_HOSTS", (function () {
            return {
                URL: {
                    ROOT_PATH: 'http://localhost/api/MySpecialApi',
                }
            }
        })());
})();
```

## Parameters

```
Replace values or parameter ${} in files.

-f|--folder              Folder to search files Examples: C:\MyFolder

-e|--extensions          A list of file extensions. Multiple values can be used
                         by joining them with separators ";"
						 Example "js,html,json"

--ignorefoldernames      A list of folder names to ignore (ex: bin, obj, .git).
                         Multiple values can be used by joining them with any
                         of the following separators: ;

--includesubfolder       Optional with default 'True'. Include sub folder in
                         the search
						 
-p|--parameters          List of parameter to replace in files. Parameter in
                         the file is ${...variable name...}. Examples:
                         --replaceparameters
                         "ParameterName1=MyValue1;ParameterName2=MyValue2;"Multi
                         ple values can be used by joining them with any of the
                         following separators: ;
						 
--parallelexecution      Optional with default '5'. Number of simultaneous parallels replacement

--nopattern              Optional. No search pattern. Usefull to find exact string.

--ignorecase             Optional. Show details during the replacement process.						 

--verbose                Optional. Show details during the replacement process.						 
```




