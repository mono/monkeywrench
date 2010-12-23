<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="User" CodeBehind="User.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
    <asp:Table CssClass="users" ID="tblUser" runat="server">
        <asp:TableRow>
            <asp:TableCell>Username:</asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtUserName" runat="server" />
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell>Full name:</asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtFullName" runat="server" />
            </asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell>Password:</asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtPassword" runat="server"></asp:TextBox>
            </asp:TableCell>
            <asp:TableCell>Passwords are stored as plain-text in the database, so don't use a personal password. All admins can also view all passwords.</asp:TableCell>
        </asp:TableRow>
        <asp:TableRow ID="rowRoles">
            <asp:TableCell>Roles:</asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtRoles" runat="server"></asp:TextBox>
            </asp:TableCell>
            <asp:TableCell>Only admins can edit. BuildBot or Administrator (or empty).</asp:TableCell>
        </asp:TableRow>
        <asp:TableRow>
            <asp:TableCell ColumnSpan="3">
                <asp:Button ID="cmdSave" Text="Save" runat="server" OnClick="cmdSave_OnClick" /></asp:TableCell>
        </asp:TableRow>
    </asp:Table>
    <div>
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
    </div>
</asp:Content>
