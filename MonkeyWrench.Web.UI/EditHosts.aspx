<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditHosts" Codebehind="EditHosts.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="EditHosts.js"></script>
    <h2>Hosts</h2>
    <div>
        <asp:Table ID="tblHosts" CssClass="buildstatus" runat="server">
        </asp:Table>
        <asp:Label ID="lblMessage" runat="server"></asp:Label>
        <br />
    </div>
</asp:Content>