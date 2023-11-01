# ReplaceInFiles
Console application to find and replace values in files. The main objective of this tool to help DevOps
with files which are not structured to be replaced with a integrated variable substitution.

The files supported are any type that can be read in text format   
**Example:** txt, json, html

The variable to find could be
- with match pattern `${}`  
- exact string (without match pattern)


## How to use it

### Find and replace using match pattern & multiple parameters
```command
"ReplaceInFiles.exe" ^
	--folder "C:\App\" ^
	--extensions "js" ^	
	--ignorefoldernames ".git,.vs,packages,jquery,kendo,limitless,plugins,node_modules" ^
	--replaceparameters "${HostName}=https://dev.mysystem.com;${HostPort}=8888" ^
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


### Find and replace exact match
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
						 
-p|--parameters          List of parameter to replace in files. Parameter in
                         the file is ${...variable name...}. Examples:
                         --replaceparameters
                         "ParameterName1=MyValue1;ParameterName2=MyValue2;"Multi
                         ple values can be used by joining them with any of the
                         following separators: ;
						 
--parallelexecution      Optional with default '5'. Number of simultaneous parallels replacement

--verbose                Optional. Verbose mode

--nopattern              Optional. No search pattern. Usefull to find raw value.

--includesubfolder       Optional with default 'True'. Include sub folder in
                         the search
						 
```




