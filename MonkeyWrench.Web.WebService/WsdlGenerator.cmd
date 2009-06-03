csc -target:library -out:WsdlGenerator.dll -keyfile:WsdlGenerator.snk WsdlGenerator.cs
gacutil -i WsdlGenerator.dll
wsdl /out:WebServices.cs /namespace:MonkeyWrench.Web.WebServices /fields /parameters:WsdlGenerator.xml WebServices.wsdl
gacutil -u WsdlGenerator
move WebServices.cs ..\MonkeyWrench.DataClasses\WebServices.Generated.cs