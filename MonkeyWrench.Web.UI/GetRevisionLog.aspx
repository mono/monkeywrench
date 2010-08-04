<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="GetRevisionLog" Codebehind="GetRevisionLog.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
		<div id="log" runat="server"></div>
		<br />
		<div id="diff" runat="server"></div>
        <br />
        <%=Request ["raw"] == "true" ? string.Format ("<a href='GetRevisionLog.aspx?id={0}'>View colorized data</a>", Request ["id"]) : string.Format ("<a href='GetRevisionLog.aspx?id={0}&amp;raw=true'>View raw data</a>", Request ["id"]) %>
</asp:Content>