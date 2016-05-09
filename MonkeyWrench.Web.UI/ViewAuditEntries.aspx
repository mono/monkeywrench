<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="ViewAuditEntries" Codebehind="ViewAuditEntries.aspx.cs" EnableSessionState="False" %>


<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
	<br>
	<h2> Audit History </h2>
	<br>
	<div id="auditList" runat="server"></div>
	<br>
	<div id="pager" runat="server" enableviewstate="false"></div>
	<br>
	<div>
	    <a href="ViewAuditEntries.aspx?limit=10">View 10 entries</a> - 
	    <a href="ViewAuditEntries.aspx?limit=50">View 50 entries</a> - 
	    <a href="ViewAuditEntries.aspx?limit=100">View 100 entries</a> - 
	    <a href="ViewAuditEntries.aspx?limit=200">View 200 entries</a> - 
	    <a href="ViewAuditEntries.aspx?limit=500">View 500 entries</a>
	</div>
</asp:Content>

