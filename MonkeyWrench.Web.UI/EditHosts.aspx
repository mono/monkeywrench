<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditHosts" CodeBehind="EditHosts.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="EditHosts.js"></script>
    <asp:Table ID="tblHosts" CssClass="buildstatus hosts" runat="server">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="4">Hosts</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Name</asp:TableHeaderCell>
            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
            <asp:TableHeaderCell>Description</asp:TableHeaderCell>
            <asp:TableHeaderCell>Architecture</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableRow>
            <asp:TableCell><input type='text' value='host' name='txtHost' id='txtHost' /></asp:TableCell>
            <asp:TableCell><a href='javascript:addHost ()'>Add</a></asp:TableCell>
            <asp:TableCell></asp:TableCell>
            <asp:TableCell></asp:TableCell>
        </asp:TableRow>
    </asp:Table>
    <div><asp:Label ID="lblMessage" runat="server" ForeColor="Red" /></div>
</asp:Content>