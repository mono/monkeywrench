<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EnvironmentVariablesEditor.ascx.cs" Inherits="MonkeyWrench.Web.UI.EnvironmentVariablesEditor" %>
<br />
Environment variables for this lane or host
<br />
You can refer to other environment variables with the ${VARIABLE} syntax.
<br />
So for instance appending /tmp to PATH would be "${PATH}:/tmp"<br />
<asp:Table ID="tblVariables" runat="server" CssClass="buildstatus">
    <asp:TableHeaderRow>
        <asp:TableHeaderCell>Name</asp:TableHeaderCell>
        <asp:TableHeaderCell>Value</asp:TableHeaderCell>
        <asp:TableHeaderCell>Action</asp:TableHeaderCell>
    </asp:TableHeaderRow>
    <asp:TableFooterRow>
        <asp:TableCell>
            <asp:TextBox ID="txtVariableName" runat="server"></asp:TextBox></asp:TableCell>
        <asp:TableCell>
            <asp:TextBox ID="txtVariableValue" runat="server"></asp:TextBox></asp:TableCell>
        <asp:TableCell>
            <asp:LinkButton ID="LinkButton1" CommandName="add" runat="server" OnCommand="EnvironmentVariable_OnCommand ">Add</asp:LinkButton></asp:TableCell>
    </asp:TableFooterRow>
</asp:Table>
