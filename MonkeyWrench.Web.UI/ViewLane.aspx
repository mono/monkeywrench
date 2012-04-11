<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="ViewLane" Codebehind="ViewLane.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
        <script type="text/javascript" src="ViewLane.js"></script>
		<div id="header" runat="server"></div>
		<div id="buildtable" runat="server">
        <asp:Label ID="lblMessage" runat="server" />
        </div>				
</asp:Content>