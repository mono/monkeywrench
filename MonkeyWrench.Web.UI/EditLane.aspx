<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="EditLane" CodeBehind="EditLane.aspx.cs" %>
<%@ Register Src="~/EnvironmentVariablesEditor.ascx"  TagName="EnvironmentVariablesEditor" TagPrefix="ucs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">

    <script type="text/javascript" src="EditLane.js"></script>
    <script type="text/javascript" src="MonkeyWrench.js"></script>

    <h2>Editing Lane: <asp:Label ID="lblH2" runat="server"></asp:Label></h2>
    <div>
        <asp:Table runat="server" CssClass="data form">
            <asp:TableRow>
                <asp:TableCell Style="width: 100px;"><label for="<%= txtID.ClientID %>">Id:</label></asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtID" runat="server">0</asp:TextBox></asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= txtLane.ClientID %>">Lane:</label></asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtLane" runat="server"></asp:TextBox></asp:TableCell><asp:TableCell>The name of this lane. Must be unique on this server.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= cmbSourceControl.ClientID %>">Source control:</label></asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="cmbSourceControl" runat="server">
                        <asp:ListItem Text="git" Value="git" />
                    </asp:DropDownList>
                </asp:TableCell>
                <asp:TableCell>Which SCM to use.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= txtRepository.ClientID %>">Repository:</label></asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtRepository" runat="server"></asp:TextBox></asp:TableCell>
                    <asp:TableCell>The repository where the code for this lane is located. The scheduler uses the revisions for this repository to schedule work. Can include multiple repositories separated with commas.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= txtCommitFilter %>">Commit filter:</label></asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtCommitFilter" runat="server"></asp:TextBox></asp:TableCell>
                    <asp:TableCell>A filter to filter out commits. An empty filter means include all commits. If not empty, the filter must start with either 'IncludeAllExcept:' or 'ExcludeAllExcept:' and followed by a semi-colon separated list of shell globs.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= txtMinRevision.ClientID %>">Min revision:</label></asp:TableCell><asp:TableCell>
                    <asp:TextBox ID="txtMinRevision" runat="server"></asp:TextBox></asp:TableCell>
                    <asp:TableCell>Revisions before this one won't get scheduled. Leave blank to include all revisions.</asp:TableCell></asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= txtMaxRevision.ClientID %>">Max revision:</label></asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtMaxRevision" runat="server"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>Revisions after this one won't get scheduled. Leave blank to include all (future) revisions. Git: If you want to track a branch other than the 'remotes/origin/master', set the revision to the remote branch you want to track (you need one max revision per repository in this case).</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= txtTags.ClientID %>">Tags:</label></asp:TableCell>
                <asp:TableCell>
                    <asp:TextBox ID="txtTags" runat="server"></asp:TextBox>
                </asp:TableCell>
                <asp:TableCell>Comma-separated list of tags for this lane. Descendant lanes do not inherit tags. Tags can contain any character except commas (in particular spaces are allowed).</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= lstParentLane.ClientID %>">Parent lane:</label></asp:TableCell>
                <asp:TableCell>
                    <asp:DropDownList ID="lstParentLane" runat="server"></asp:DropDownList>
                </asp:TableCell>
                <asp:TableCell>The (optional) parent lane of this lane.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell>Child lanes:</asp:TableCell>
                <asp:TableCell>
                    <asp:Label ID="lblChildLanes" runat="server"></asp:Label>
                </asp:TableCell>
                <asp:TableCell></asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= chkTraverseMerges.ClientID %>">Traverse merges:</label></asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkTraverseMerges" runat="server"></asp:CheckBox>
                </asp:TableCell>
                <asp:TableCell>If merged-in commits (except the merge commit itself, which is always included) should be included.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell><label for="<%= chkEnabled.ClientID %>">Enabled:</label></asp:TableCell>
                <asp:TableCell>
                    <asp:CheckBox ID="chkEnabled" runat="server"></asp:CheckBox>
                </asp:TableCell>
                <asp:TableCell>If a lane is enabled. If not enabled nothing will be done for it and it won't show up on the front page.</asp:TableCell>
            </asp:TableRow>
            <asp:TableRow>
                <asp:TableCell ColumnSpan="2">
                    <asp:Button ID="cmdSave" runat="server" Text="Save" OnClick="cmdSave_Click" CssClass="save" />
                    <asp:Button ID="cmdDeleteAllWork" runat="server" Text="Delete all work" OnClick="cmdDeleteAllWork_Click" />
                    <asp:Button ID="cmdClearAllWork" runat="server" Text="Clear all work" OnClick="cmdClearAllWork_Click" />
                    <asp:Button ID="cmdDeleteAllRevisions" runat="server" Text="Delete all revisions" OnClick="cmdDeleteAllRevisions_Click" />
                    <asp:Button id="cmdDontDoWork" runat="server" Text="Mark all pending revisions as 'Don't build'" OnClick="cmdDontDoWork_Click" />
                 </asp:TableCell>
            </asp:TableRow>
        </asp:Table>

        <asp:Table runat="server" ID="tblDependencies" CssClass="data formset">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell ColumnSpan="6" CssClass="title">Dependencies</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Dependent lane</asp:TableHeaderCell>
                <asp:TableHeaderCell>Condition</asp:TableHeaderCell>
                <asp:TableHeaderCell>Host</asp:TableHeaderCell>
                <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
                <asp:TableHeaderCell>Files to download</asp:TableHeaderCell>
                <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <asp:Table runat="server" ID="tblCommands" CssClass="data formset">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell CssClass="title" ColumnSpan="12">Commands</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Sequence</asp:TableHeaderCell>
                <asp:TableHeaderCell>Command</asp:TableHeaderCell>
                <asp:TableHeaderCell>Always Execute</asp:TableHeaderCell>
                <asp:TableHeaderCell>Non fatal</asp:TableHeaderCell>
                <asp:TableHeaderCell>Internal</asp:TableHeaderCell>
                <asp:TableHeaderCell>Executable</asp:TableHeaderCell>
                <asp:TableHeaderCell>Arguments</asp:TableHeaderCell>
                <asp:TableHeaderCell>Timeout</asp:TableHeaderCell>
                <asp:TableHeaderCell>Working Directory</asp:TableHeaderCell>
                <asp:TableHeaderCell>Upload files</asp:TableHeaderCell>
                <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                <asp:TableHeaderCell>Notes</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <asp:Table runat="server" ID="tblFiles" CssClass="data formset">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell CssClass="title" ColumnSpan="4">Files</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Filename</asp:TableHeaderCell>
                <asp:TableHeaderCell>Mime</asp:TableHeaderCell>
                <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
                <asp:TableHeaderCell>Used in</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <asp:Table runat="server" ID="tblHosts" CssClass="data formset">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell CssClass="title" ColumnSpan="2">Hosts</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Host</asp:TableHeaderCell>
                <asp:TableHeaderCell>Actions</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <asp:Table runat="server" CssClass="data formset">
            <asp:TableHeaderRow CssClass="title">
                <asp:TableHeaderCell>Environment Variables</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableRow>
                <asp:TableCell>
                    <ucs:EnvironmentVariablesEditor ID="editorVariables" runat="server"></ucs:EnvironmentVariablesEditor>
                </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Table ID="tblNotifications" runat="server"  CssClass="data formset">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell CssClass="title" ColumnSpan="2">Notifications</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Name</asp:TableHeaderCell><asp:TableHeaderCell>Type</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableFooterRow>
                <asp:TableCell>
                    <asp:DropDownList ID="cmbNotifications" runat="server" /> 
                </asp:TableCell>
                <asp:TableCell>
                    <asp:LinkButton ID="lnkAddNotification" runat="server" Text="Add" OnClick="lnkAddNotification_Click" />
                </asp:TableCell>
            </asp:TableFooterRow>
        </asp:Table>

        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
    </div>
</asp:Content>
