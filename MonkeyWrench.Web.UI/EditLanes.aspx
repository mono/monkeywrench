<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditLanes" Codebehind="EditLanes.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="EditLanes.js"></script>
    <h2>Lanes</h2>
    <div>
        <asp:Table ID="tblLanes" runat="server" CssClass="buildstatus"></asp:Table>
        <asp:Label ID="lblMessage" runat="server"></asp:Label>
    </div>
</asp:Content>
