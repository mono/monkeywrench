<%@ Page Language="C#" MasterPageFile="~/Master.master" Inherits="MonkeyWrench.Web.UI.ErrorPage" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
	<h2><asp:Label id="errorHeader" runat="server"></asp:Label></h2>
	<p><asp:Label id="errorDescription" runat="server"></asp:Label></p>
</asp:Content>
