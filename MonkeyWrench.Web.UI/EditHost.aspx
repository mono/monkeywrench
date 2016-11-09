<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="EditHost" CodeBehind="EditHost.aspx.cs" %>

<%@ Register Src="~/EnvironmentVariablesEditor.ascx" TagName="EnvironmentVariablesEditor" TagPrefix="ucs" %>
<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">

    <script type="text/javascript" src="EditHost.js"></script>

    <script type="text/javascript" src="MonkeyWrench.js"></script>

    <div>
        <br />
        <br />
        <asp:Table ID="tblData" runat="server">
            <asp:TableRow>
                <asp:TableCell>
                    Id:
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtID" runat="server" Width="471px"></asp:TextBox>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    Host:
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtHost" runat="server" Width="471px"></asp:TextBox>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    Password:
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtPassword" runat="server" Width="471px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>
                    This is the password the host uses to log into the web service.
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    Queue management:
                </asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="cmbQueueManagement" runat="server" Width="471px">
                        <asp:ListItem Text="Completely finish a revision before starting a new one" Value="0"></asp:ListItem>
                        <!-- This is not implemented
                        <asp:ListItem Text="Execute last revision as soon as possible" Value="1"></asp:ListItem>
                        -->
                        <asp:ListItem Text="Execute one revision per lane at a time" Value="2"></asp:ListItem>
                    </asp:DropDownList>
                </asp:TableCell>
                <asp:TableCell>
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    Description:
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtDescription" runat="server" Width="471px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>
                    (informational purposes only)
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    Architecture:
                </asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtArchitecture" runat="server" Width="471px"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>
                    (informational purposes only)
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                    Enabled
                </asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkEnabled" runat="server" />
                </asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>
                </asp:TableCell>
                <asp:TableCell>
                    <asp:Button ID="cmdSave" runat="server" Text="Save" OnClick="cmdSave_Click" />
                    <asp:Button id="cmdDeleteAllWork" runat="server" Text="Delete all work" OnClick="cmdDeleteAllWork_Click" />
                    <asp:Button id="cmdClearAllWork" runat="server" Text="Clear all work" OnClick="cmdClearAllWork_Click" />
                </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Label ID="lblPasswordWarning" runat="server" ForeColor="Tomato" />
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
        <br />
        Lanes configured for this host:<br />
        <br />
        <asp:Table ID="tblLanes" runat="server" CssClass="buildstatus">
        </asp:Table>
        <br />
        Master hosts this host will work for:<br />
        If no hosts are specified it will work for itself, if there are any master hosts specified, you need to add itself to the list below for it to work for itself.
        <br />
        <asp:Table ID="tblMasters" runat="server" CssClass="buildstatus">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Master Host</asp:TableHeaderCell><asp:TableHeaderCell>Action</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableRow>
                <asp:TableCell>
                    <asp:DropDownList ID="cmbMasterHosts" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:LinkButton runat="server" ID="cmdAddMasterHost" CommandName="AddMasterHost" OnCommand="OnLinkButtonCommand">Add</asp:LinkButton>
                </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <br />
        Slave hosts of this host:
        <br />
        <asp:Table ID="tblSlaves" runat="server" CssClass="buildstatus">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Slave Host</asp:TableHeaderCell><asp:TableHeaderCell>Action</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <br />
        <ucs:EnvironmentVariablesEditor ID="editorVariables" runat="server"></ucs:EnvironmentVariablesEditor>
        <br />
        Sample MonkeyWrench.xml configuration file (copy to ~/.config/MonkeyWrench/MonkeyWrench.xml):
        <div ID="lblConfiguration" runat="server" />
    </div>
</asp:Content>
