
Imports System.Data
Imports System.IdentityModel.Protocols.WSTrust
Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports CCA.Util
Imports ClosedXML.Excel

Partial Class APPS_Services_PaymentCollection
    Inherits System.Web.UI.Page

    Dim objTopup As clsTopup = New clsTopup()
    Dim objUser As clsUser = New clsUser()
    Dim ccaCrypto As CCACrypto = New CCACrypto()

    Public strEncRequest As String = ""
    Public strAccessCode As String = ConfigurationManager.AppSettings("CCAvenueAccessCode")
    Public strWorkingKey As String = ConfigurationManager.AppSettings("CCAvenueWorkingKey")
    Public strMerchantId As String = ConfigurationManager.AppSettings("CCAvenueMerchandId")
    Public strPaymentURL As String = ConfigurationManager.AppSettings("CCAvenuePaymentURL")

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Try
            If Not IsPostBack Then
                FillProductDropDown()
                ceInstrDate.EndDate = DateTime.Now
                pnlPaymentCollection.Visible = True
                pnlProductDetails.Visible = False
                pnlPaymentHistory.Visible = False
            End If
        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "Page_Load")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Protected Sub FillProductDropDown()
        Try
            Dim productlist = objTopup.CollectionProductList(0)

            If productlist IsNot Nothing And productlist.Rows.Count > 0 Then
                ddlProduct.Items.Clear()
                ddlProduct.DataSource = productlist
                ddlProduct.DataTextField = "ProductName"
                ddlProduct.DataValueField = "ProductID"
                ddlProduct.DataBind()
            End If
            ddlProduct.Items.Insert(0, (New ListItem("--Select Product--", "0")))
        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "Select_Product")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Protected Sub ddlProduct_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlProduct.SelectedIndexChanged
        Try
            If rbtnpaytype.SelectedValue.ToString() = "1" Then
                Dim productid = Convert.ToInt32(ddlProduct.SelectedValue)

                pnlProductDetails.Visible = False
                trtransactionid.Visible = False
                trpaymentDate.Visible = False
                trfileupload.Visible = False
                trpaymentservice.Visible = False
                trRemarks.Visible = True
                btnPayment.Text = "Pay Now"
                If productid > 0 Then
                    Dim productlist = objTopup.CollectionProductList(productid)

                    If productlist IsNot Nothing And productlist.Rows.Count > 0 Then
                        Dim dr = productlist.Rows(0)

                        If dr IsNot Nothing Then
                            Dim amount = Decimal.Parse(dr("Amount").ToString())
                            txtAmount.Text = Math.Round(amount, 2)
                            pnlProductDetails.Visible = True
                        End If
                    End If
                End If
            Else
                Dim productid = Convert.ToInt32(ddlProduct.SelectedValue)

                pnlProductDetails.Visible = True
                trtransactionid.Visible = True
                trpaymentDate.Visible = True
                trfileupload.Visible = True
                trpaymentservice.Visible = True
                trRemarks.Visible = False
                btnPayment.Text = "Submit"
                If productid > 0 Then
                    Dim productlist = objTopup.CollectionProductList(productid)

                    If productlist IsNot Nothing And productlist.Rows.Count > 0 Then
                        Dim dr = productlist.Rows(0)

                        If dr IsNot Nothing Then
                            Dim amount = Decimal.Parse(dr("Amount").ToString())
                            txtAmount.Text = Math.Round(amount, 2)
                            pnlProductDetails.Visible = True
                        End If
                    End If
                End If
            End If
            ddlpaymentservice.SelectedValue = "0"
            txtTransactionID.Text = ""
            txtpaymentDate.Text = ""
            txtAgentCode.Text = ""
            txtAgentName.Text = ""

            txtpaymentDate.Text = ""


        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "ddlProduct_SelectedIndexChanged")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Protected Sub SearchAgenctCode(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtAgentCode.TextChanged
        Try
            txtAgentName.Text = String.Empty
            Dim agentcode = txtAgentCode.Text

            If Not String.IsNullOrEmpty(agentcode) Then
                If agentcode.Length >= 6 Then
                    Dim dt = objUser.ListEntity(txtAgentCode.Text)
                    If (dt IsNot Nothing And dt.Rows.Count > 0) Then
                        txtAgentName.Text = dt.Rows(0)("username").ToString()
                    Else
                        Say(Me.Page, "No Record Found", True)
                        txtAgentCode.Text = String.Empty
                    End If
                Else
                    Say(Me.Page, "Invalid Agent Code", True)
                    txtAgentCode.Text = String.Empty
                End If
            End If
        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "SearchAgenctCode")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub


    Protected Sub PaymentRequest(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim userid = Session("Userinfo")(0)
            Dim amount = Decimal.Parse(txtAmount.Text)
            Dim PaymentModex = "PAYMENTGATEWAY"
            Dim remarks = txtRemarks.Text
            Dim refagentcode = txtAgentCode.Text
            Dim productid = Convert.ToInt32(ddlProduct.SelectedValue)
            Dim CollectionID As String = ""

            If btnPayment.Text = "Pay Now" Then
                CollectionID = objTopup.PaymentProdCollectionBeta(userid, amount, 0, PaymentModex, remarks, userid, productid, refagentcode)

                If Not String.IsNullOrEmpty(CollectionID) Then

                    Dim tid = DateTime.Now.Ticks.ToString()
                    Dim urlhost = Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath


                    Dim params = ""
                    params = String.Concat(params, "tid=", tid, "&")
                    params = String.Concat(params, "merchant_id=", strMerchantId, "&")
                    params = String.Concat(params, "order_id=", CollectionID, "&")
                    params = String.Concat(params, "amount=", amount, "&")
                    params = String.Concat(params, "currency=", "INR", "&")
                    params = String.Concat(params, "redirect_url=", String.Concat(urlhost, "ccavResponseHandlerWeb.aspx"), "&")
                    params = String.Concat(params, "cancel_url=", String.Concat(urlhost, "ccavResponseHandlerWeb.aspx"), "&")

                    strEncRequest = ccaCrypto.Encrypt(params.TrimEnd("&"), strWorkingKey)

                    postPaymentRequestToGateway(strPaymentURL, strEncRequest)

                    ddlProduct.SelectedValue = "0"
                    ddlProduct_SelectedIndexChanged(sender, e)

                    txtAgentCode.Text = String.Empty
                    txtAgentName.Text = String.Empty
                    txtRemarks.Text = String.Empty

                Else
                    Say(Me.Page, "Payment Collection Failed", True)
                End If
            Else
                Dim filepathPhoto As String = ""
                Dim fileSignaturepth As String = ""
                If txtpaymentDate.Text = "" Then
                    Say(Me.Page, "Payment Date is Required", True)
                    Exit Sub
                End If
                If txtTransactionID.Text = "" Then
                    Say(Me.Page, "Transaction id is Required", True)
                    Exit Sub
                End If
                If txtTransactionID.Text = "0" Then
                    Say(Me.Page, "Transaction id is Required", True)
                    Exit Sub
                End If
                If ddlpaymentservice.SelectedValue = "0" Then
                    Say(Me.Page, "Payment service type  is Required", True)
                    Exit Sub
                End If
                If ddlProduct.SelectedValue = "0" Then
                    Say(Me.Page, "Product type  is Required", True)
                    Exit Sub
                End If

                If flpupload.HasFile Then
                    Dim folderPath As String = Server.MapPath("~/Paymentcollection/")
                    If Not Directory.Exists(folderPath) Then
                        Directory.CreateDirectory(folderPath)
                    End If
                    fileSignaturepth = Session("UserInfo")(0).ToString() & "_" & ddlProduct.SelectedValue.ToString() & "_" & txtTransactionID.Text + Path.GetExtension(flpupload.PostedFile.FileName)
                    'fileSignaturepth = DateTime.Now.ToString("ddMMyyyy_HHmmss") & "_" & Session("UserInfo")(1) & "_" & Path.GetFileName(flpupload.FileName)
                    filepathPhoto = "~/Paymentcollection/" & fileSignaturepth
                    Dim filephotosrv As String = Server.MapPath(filepathPhoto)
                    flpupload.SaveAs(filephotosrv)

                Else
                    Say(Me.Page, "Please Upload Photo", True)
                    Exit Sub
                End If

                CollectionID = objTopup.PaymentProdCollectionAlreadyPaid(userid, amount, 0, txtTransactionID.Text, "", userid, productid, refagentcode, ddlpaymentservice.SelectedValue.ToString(), txtpaymentDate.Text, fileSignaturepth)
                txtAgentCode.Text = String.Empty
                txtAgentName.Text = String.Empty
                txtRemarks.Text = String.Empty
                txtpaymentDate.Text = String.Empty
                txtTransactionID.Text = String.Empty
                Say(Me.Page, "Request Successfully Submitted ", True)
            End If



        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "PaymentRequest")
                'Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Private Sub postPaymentRequestToGateway(ByVal queryUrl As [String], ByVal enCryptData As [String])
        Try
            Dim javascriptfunction As String = String.Concat("paymentsubmit('", queryUrl, "','", strAccessCode, "','", enCryptData, "')")
            ScriptManager.RegisterClientScriptBlock(Page, GetType(Page), "Script", javascriptfunction, True)


            'Dim collections As New NameValueCollection()
            'collections.Add("encRequest", enCryptData)
            'collections.Add("access_code", strAccessCode)

            'Dim html As String = "<html><head>"
            'html += "</head><body onload='document.forms[0].submit(); location.href=location.href;'>"
            'html += String.Format("<form name='PostForm' target='_blank' method='POST' name='redirect' action='{0}'>", queryUrl)
            'For Each key As String In collections.Keys
            '    html += String.Format("<input name='{0}' type='text' value='{1}'>", key, collections(key))
            'Next
            'html += "</form></body></html>"
            'Response.Clear()
            'Response.ContentEncoding = Encoding.GetEncoding("ISO-8859-1")
            'Response.HeaderEncoding = Encoding.GetEncoding("ISO-8859-1")
            'Response.Charset = "ISO-8859-1"
            'Response.Write(html)
            'Response.Flush()
            'Response.End()
        Catch exception As Exception
            Throw exception
        End Try
    End Sub

    Protected Sub btnExport_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExport.Click
        Dim objGrid As DataGrid
        Dim objSW As System.IO.StringWriter
        Dim objHtmlWriter As System.Web.UI.HtmlTextWriter
        Try
            Response.Clear()
            Response.ClearContent()
            Response.ContentType = "application/vnd.xls"
            Response.AddHeader("content-disposition", "attachment; filename=PaymentHistoryReport.xls")
            Response.Charset = ""
            '
            objGrid = New DataGrid
            objGrid.DataSource = ViewState("Result")
            '// objGrid.AllowPaging = False
            objGrid.DataBind()
            '
            objSW = New System.IO.StringWriter()
            objHtmlWriter = New HtmlTextWriter(objSW)
            objGrid.RenderControl(objHtmlWriter)
            Response.Write(objSW)
            Response.Flush()
            Response.End()
        Catch ex As Exception

        End Try
    End Sub
    Public Function Encrypt(ByVal strText As String, ByVal Publickey As String) As String
        Try
            Dim databyte = Encoding.UTF8.GetBytes(strText)

            Dim keyXml = "<RSAKeyValue><Modulus>" + Publickey + "</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>"

            Using Rsa = New RSACryptoServiceProvider()
                Rsa.FromXmlString(keyXml.ToString())

                Dim encryptedData = Rsa.Encrypt(databyte, True)

                Dim base64Encrypted = Convert.ToBase64String(encryptedData)

                Return base64Encrypted
            End Using
        Catch ex As Exception
            Throw New Exception("Encrypt(String): " & ex.Message, ex)
        End Try
    End Function


    Protected Sub CancelPayment(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            pnlProductDetails.Visible = False
            ddlProduct.SelectedValue = 0
        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "CancelPayment")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Protected Sub GetPaymentHistory(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            pnlPaymentCollection.Visible = False
            pnlPaymentHistory.Visible = True

            BindGrid()
        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "GetPaymentHistory")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Private Sub BindGrid()
        Dim objDT As DataTable = New DataTable()
        Try
            objDT = objTopup.CollectionPaymentList(Session("Userinfo")(0))
            ViewState("Result") = objDT

            If objDT IsNot Nothing And objDT.Rows.Count > 0 Then
                grdPaymentHistory.DataSource = objDT
                grdPaymentHistory.DataBind()
            Else
                grdPaymentHistory.DataSource = Nothing
                grdPaymentHistory.DataBind()
            End If

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Protected Sub btnBack_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            pnlPaymentCollection.Visible = True
            pnlPaymentHistory.Visible = False
        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "btnBack_Click")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub

    Protected Sub rbtnpaytype_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles rbtnpaytype.SelectedIndexChanged
        Try
            If rbtnpaytype.SelectedValue.ToString() = "1" Then
                Dim productid = Convert.ToInt32(ddlProduct.SelectedValue)

                pnlProductDetails.Visible = False
                trtransactionid.Visible = False
                trpaymentDate.Visible = False
                trfileupload.Visible = False
                trpaymentservice.Visible = False
                trRemarks.Visible = True
                btnPayment.Text = "Pay Now"

                txtAgentCode.Text = ""
                txtAgentName.Text = ""
                txtAmount.Text = ""
                txtpaymentDate.Text = ""
                txtRemarks.Text = ""
                txtTransactionID.Text = ""
                ddlpaymentservice.SelectedValue = "0"
                ddlProduct.SelectedValue = "0"
            Else
                Dim productid = Convert.ToInt32(ddlProduct.SelectedValue)

                pnlProductDetails.Visible = True
                trtransactionid.Visible = True
                trpaymentDate.Visible = True
                trfileupload.Visible = True
                trpaymentservice.Visible = True
                trRemarks.Visible = False
                btnPayment.Text = "Submit"

                txtAgentCode.Text = ""
                txtAgentName.Text = ""
                txtAmount.Text = ""
                txtpaymentDate.Text = ""
                txtRemarks.Text = ""
                txtTransactionID.Text = ""
                ddlpaymentservice.SelectedValue = "0"
                ddlProduct.SelectedValue = "0"
            End If


        Catch ex As Exception
            If ex.Message.StartsWith("{*}") Then
                Say(Me.Page, ex.Message, True)
            Else
                LogError(ex.Message.ToString(), Me.Page.Title, "ddlProduct_SelectedIndexChanged")
                Response.Redirect(gcsErrorURL, False)
            End If
        End Try
    End Sub
End Class
