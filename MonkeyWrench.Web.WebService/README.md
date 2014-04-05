If the types used in the API in WebServices.asmx is modified (for instance if a type used as
a return type for a [WebMethod] method gains a new public field), then 
MonkeyWrench.Web.WebService/WebServices.wsdl must be updated. A brave soul can accomplish this
by editing the file manually, but it's easier to just run the process described below.

If the API in WebServices.asmx is modified (new [WebMethod] method,
modified signature of existing method, etc), then MonkeyWrench.DataClasses/WebServices.Generated.cs
must be updated. A *very* brave soul may be able to accomplish this by editing the file
manually.

MonkeyWrench.Web.WebService/WebServices.wsdl
--------------------------------------------

On a Mac or Linux, execute the following in a terminal:

    make web

Then in another terminal:

    make wsdl

Note that this may fail complaining about dos2linux (or linux2dos) - you can ignore this.

MonkeyWrench.DataClasses/WebServices.Generated.cs
-------------------------------------------------

Prequisites:
* A Mac or Linux machine with monkeywrench checked out and an updated 
  MonkeyWrench.Web.WebService/WebServices.wsdl from the step above.
* This Mac/Linux machine must share the monkeywrench directory on the network (r/w).
* A Windows machine with access to said network.

Open a VS20XX Command Line prompt with admin privileges, and execute the following:

     net use X: \\ip-of-mac-computer-where-monkeywrench-is-checked-out\samba-share
     X:
     cd path\to\checkout\of\monkeywrench\MonkeyWrench.Web.WebServices
     WsdlGenerator.cmd
