<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="GetRevisionLog" Codebehind="GetRevisionLog.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
		<pre id="log" runat="server"></pre>
		<br />
		<pre id="diff" runat="server"></pre>
</asp:Content>