<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="Identities"
    CodeBehind="Identities.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
    <script type="text/javascript" src="Identities.js"></script>

    <asp:Table ID="tblIrcIdentities" runat="server" CssClass="data index">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="8" CssClass="title">IRC Identities</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Name</asp:TableHeaderCell>
            <asp:TableHeaderCell>Servers</asp:TableHeaderCell>
            <asp:TableHeaderCell>Password</asp:TableHeaderCell>
            <asp:TableHeaderCell>Use SSL</asp:TableHeaderCell>
            <asp:TableHeaderCell>Channels</asp:TableHeaderCell>
            <asp:TableHeaderCell>Join Channels</asp:TableHeaderCell>
            <asp:TableHeaderCell>Nicks</asp:TableHeaderCell>
            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableFooterRow>
            <asp:TableCell>
                <asp:TextBox ID="txtIrcName" runat="server" ToolTip="&nbsp;"/>
            </asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtIrcServers" runat="server" ToolTip="A comma separated list of irc servers" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtPassword" runat="server" ToolTip="The password for the server(s) (if any)" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:CheckBox ID="chkUseSsl" Text="" runat="server" ToolTip="If the server(s) require SSL" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtIrcChannels" runat="server" ToolTip="A comma separated list of irc channels to join" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:CheckBox ID="chkJoinChannels" Text="" runat="server" ToolTip="If monkeywrench should join the channels." />
            </asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtIrcNicks" runat="server" ToolTip="A comma separated list of irc nicks to use" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:LinkButton ID="lnkIrcAdd" Text="Add" runat="server" OnClick="lnkIrcAdd_Click" />
            </asp:TableCell>
        </asp:TableFooterRow>
    </asp:Table>

    <div id="lblIrcHelp" style="color: Green">&nbsp;</div>

    <asp:Table ID="tblEmailIdentities" runat="server" CssClass="data index">
        <asp:TableHeaderRow>
            <asp:TableHeaderCell ColumnSpan="4" CssClass="title">Email Identities</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableHeaderRow>
            <asp:TableHeaderCell>Name</asp:TableHeaderCell>
            <asp:TableHeaderCell>Email</asp:TableHeaderCell>
            <asp:TableHeaderCell>Password</asp:TableHeaderCell>
            <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
        </asp:TableHeaderRow>
        <asp:TableFooterRow>
            <asp:TableCell>
                <asp:TextBox ID="txtEmailName" runat="server" ToolTip="&nbsp;" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtEmailEmail" runat="server" ToolTip="The email address used to send email" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:TextBox ID="txtEmailPassword" runat="server" ToolTip="The password for the email address" />
            </asp:TableCell>
            <asp:TableCell>
                <asp:LinkButton ID="lnkEmailAdd" Text="Add" runat="server" OnClick="lnkEmailAdd_Click" />
            </asp:TableCell>
        </asp:TableFooterRow>
    </asp:Table>

    <div id="lblEmailHelp" style="color: Green">&nbsp;</div>

    <asp:Label ID="lblMessage" ForeColor="Red" runat="server" />
</asp:Content>
