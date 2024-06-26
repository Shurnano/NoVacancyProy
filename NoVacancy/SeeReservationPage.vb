﻿Imports MySqlConnector

Public Class SeeReservationPage
    Dim server = "localhost"
    Dim user = "root"
    Dim pwd = "root"
    Dim database = "hotel"
    Dim connectionString = "server=" & server & ";user=" & user & ";password=" & pwd & ";database=" & database
    Public reservationId As Integer
    Dim startDate As Date
    Dim endDate As Date
    Dim newEndDate As Date
    Public roomId As Integer
    Dim globalProduct As String
    Dim previousMinus As Integer
    Dim previousPlus As Integer
    Dim productList As New List(Of Tuple(Of Integer, String))()
    Dim resTotalPrice As Double
    Dim nightPrice As Double

#Region "OnLoad region"
    Private Sub EditReservationForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ShowReservation()
        FillComboBoxProducts()
        ShowReservationPrice()
        CheckInvoiceExistence()
    End Sub
#End Region

#Region "Button events region"
    'Llama a la función AddProductsToReservation
    Private Sub Btn_AddProducts_Click(sender As Object, e As EventArgs) Handles Btn_AddProducts.Click
        AddProductsToReservation()
    End Sub
    'Llama a la función EditReservation
    Private Sub Btn_EditReservation_Click(sender As Object, e As EventArgs) Handles Btn_EditReservation.Click
        startDate = TxtBox_Startdate.Text
        newEndDate = TxtBox_Enddate.Text
        EditReservation()
    End Sub

    'Llama a la función DeleteReservation
    Private Sub Btn_DeleteReservation_Click(sender As Object, e As EventArgs) Handles Btn_DeleteReservation.Click
        DeleteReservation()
    End Sub

    'Decrementa la cantidad de productos y actualiza la lista de productos de la reserva.
    Private Sub Btn_Minus_Click(sender As Object, e As EventArgs) Handles Btn_Minus.Click
        If TxtBox_Quantity.Text IsNot Nothing AndAlso TxtBox_Quantity.Text > 0 Then
            Dim quantity As Integer = TxtBox_Quantity.Text
            TxtBox_Quantity.Text = quantity - 1
            If ListBox_ReservationProds.Items.Contains(globalProduct & " " & quantity) Then
                previousMinus = ListBox_ReservationProds.Items.IndexOf(globalProduct & " " & quantity)
                ListBox_ReservationProds.Items.RemoveAt(previousMinus)
                ListBox_ReservationProds.Items.Add(globalProduct & " " & quantity - 1)
            Else
                ListBox_ReservationProds.Items.Add(globalProduct & " " & quantity - 1)
            End If

        End If
    End Sub

    'Aumenta la cantidad de productos y actualiza la lista de productos de la reserva.
    Private Sub Btn_Plus_Click(sender As Object, e As EventArgs) Handles Btn_Plus.Click
        If TxtBox_Quantity.Text IsNot Nothing Then
            Dim quantity As Integer = TxtBox_Quantity.Text
            TxtBox_Quantity.Text = quantity + 1
            If ListBox_ReservationProds.Items.Contains(globalProduct & " " & quantity) Then
                previousPlus = ListBox_ReservationProds.Items.IndexOf(globalProduct & " " & quantity)
                ListBox_ReservationProds.Items.RemoveAt(previousPlus)
                ListBox_ReservationProds.Items.Add(globalProduct & " " & quantity + 1)
            Else
                ListBox_ReservationProds.Items.Add(globalProduct & " " & quantity + 1)
            End If

        End If
    End Sub

    'Finaliza la reserva y genera la factura
    Private Sub Btn_EndReservation_Click(sender As Object, e As EventArgs) Handles Btn_EndReservation.Click
        InsertInvoice()
        InvoiceReportPage.ShowDialog()
        Me.Close()
    End Sub

    'Borra los productos seleccioandos
    Private Sub Btn_DeleteProduct_Click(sender As Object, e As EventArgs) Handles Btn_DeleteProduct.Click
        If ListBox_ReservationProds.SelectedItems.Count > 0 Then
            ListBox_ReservationProds.Items.Remove(ListBox_ReservationProds.SelectedItem)
        End If
    End Sub


#End Region

#Region "other control events"
    'Habilita el botón de borrar si hay un producto seleccioando
    Private Sub ListBox_ReservationProds_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox_ReservationProds.SelectedIndexChanged
        If ListBox_ReservationProds.SelectedItem IsNot Nothing Then
            Btn_DeleteProduct.Enabled = True
        Else
            Btn_DeleteProduct.Enabled = False
        End If
    End Sub
    'Llama a la funcion GetProductQuantityInReservation
    Private Sub CB_Products_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CB_Products.SelectedIndexChanged
        GetProductQuantityInReservation(CB_Products.Text)
    End Sub
#End Region

#Region "Main subs and functions region"
    'Función que muestra en el txtbox correspondeinte el precio de la reserva. 
    Private Sub ShowReservationPrice()
        Dim query As String = "SELECT precio_reserva FROM Reserva WHERE id_reserva = @reservationId"
        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                Try
                    connection.Open()
                    command.Parameters.AddWithValue("@reservationId", reservationId)
                    Dim reader As MySqlDataReader = command.ExecuteReader()
                    While reader.Read()
                        TxtBox_TotalAmount.Text = reader("precio_reserva")
                    End While

                    reader.Close()
                    connection.Close()
                Catch ex As Exception
                    MessageBox.Show("Error al editar la reserva: " & ex.Message)
                End Try
            End Using
        End Using
    End Sub

    'Función para mostrar la reserva en el grid
    Private Sub ShowReservation()
        Dim query As String = " SELECT h.numero_habitacion, 
                                h.tipo,
                                r.fecha_inicio,
                                r.fecha_fin,
                                h.precio
                                FROM Habitacion h
                                LEFT JOIN Reserva r ON h.id_habitacion = r.id_habitacion
                                WHERE r.id_reserva = @reservation_id"

        Using connection As New MySqlConnection(connectionString)
            Dim adapter As New MySqlDataAdapter(query, connection)
            adapter.SelectCommand.Parameters.AddWithValue("@reservation_id", reservationId)
            Dim dataTable As New DataTable()
            adapter.Fill(dataTable)

            If dataTable.Rows.Count > 0 Then
                DataGridView1.DataSource = dataTable
                FillTextBox()
            Else
                MessageBox.Show("No se encontraron reservas para el ID especificado.")
            End If
        End Using
    End Sub

    'Función que rellena los textbox necesarios
    Private Sub FillTextBox()
        If DataGridView1.Rows.Count > 0 Then
            Dim firstRow As DataGridViewRow = DataGridView1.Rows(0)
            TxtBox_Room.Text = Convert.ToInt32(firstRow.Cells("numero_habitacion").Value)
            TxtBox_Type.Text = firstRow.Cells("tipo").Value.ToString()
            startDate = firstRow.Cells("fecha_inicio").Value
            TxtBox_Startdate.Text = startDate.ToString("yyyy-MM-dd")
            endDate = firstRow.Cells("fecha_fin").Value
            TxtBox_Enddate.Text = endDate.ToString("yyyy-MM-dd")
            nightPrice = Convert.ToDouble(firstRow.Cells("precio").Value)

        End If
    End Sub

    'Función para editar la reserva si el usuario confirma el mensaje
    Private Sub EditReservation()
        Dim result As DialogResult = MessageBox.Show("¿Está seguro de que desea editar esta reserva?", "Confirmar edición", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
        If result = DialogResult.OK Then
            Dim query As String = " UPDATE Reserva
                                    SET fecha_inicio = @startdate, fecha_fin = @enddate, precio_reserva = precio_reserva + @sumDays
                                    WHERE id_reserva = @id_reserva
                                    AND id_habitacion = @id_habitacion
                                    "
            Using connection As New MySqlConnection(connectionString)
                Using command As New MySqlCommand(query, connection)
                    command.Parameters.AddWithValue("@startdate", startDate.ToString("yyyy-MM-dd"))
                    command.Parameters.AddWithValue("@enddate", newEndDate.ToString("yyyy-MM-dd"))
                    command.Parameters.AddWithValue("@id_reserva", reservationId)
                    command.Parameters.AddWithValue("@id_habitacion", roomId)
                    Dim daysDiff As Double = newEndDate.Day - endDate.Day
                    Dim addAmount = daysDiff * nightPrice
                    command.Parameters.AddWithValue("@sumDays", addAmount)
                    Try
                        connection.Open()
                        If CheckDisponibility() = True Then
                            Dim rowsAffected As Integer = command.ExecuteNonQuery()
                        Else
                            MessageBox.Show("No se encontraron reservas para el ID especificado.")
                        End If
                    Catch ex As Exception
                        MessageBox.Show("Error al editar la reserva: " & ex.Message)
                    End Try
                End Using
            End Using
            Me.Close()
        End If
    End Sub

    'Función para revisar la disponibilidad de la reserva editada, devuelve false si encuentra un registro, true si no
    Public Function CheckDisponibility()
        Dim avaiable As Boolean = False
        Dim query As String = " SELECT *
                                FROM Reserva
                                WHERE id_habitacion = @id_habitacion
                                AND id_reserva <> @id_reserva
                                AND (
                                    (2024-05-01 BETWEEN fecha_inicio AND fecha_fin)
                                    OR (2024-05-07 BETWEEN fecha_inicio AND fecha_fin)
                                    OR (fecha_inicio BETWEEN 2024-05-01 AND 2024-05-07)  -- Nueva condición
                                    OR (fecha_fin BETWEEN 2024-05-01 AND 2024-05-07)     -- Nueva condición
                                );"
        Using connection As New MySqlConnection(connectionString)
            Dim adapter As New MySqlDataAdapter(query, connection)
            adapter.SelectCommand.Parameters.AddWithValue("@startdate", startDate.ToString("yyyy-MM-dd"))
            adapter.SelectCommand.Parameters.AddWithValue("@enddate", endDate.ToString("yyyy-MM-dd"))
            adapter.SelectCommand.Parameters.AddWithValue("@id_reserva", reservationId)
            adapter.SelectCommand.Parameters.AddWithValue("@id_habitacion", roomId)

            Dim dataTable As New DataTable()

            Try
                adapter.Fill(dataTable)
                If dataTable.Rows.Count > 0 Then
                    avaiable = False
                Else
                    avaiable = True
                End If

            Catch ex As Exception
                MessageBox.Show("Error al editar la reserva: " & ex.Message)
            End Try
        End Using
        Return avaiable
    End Function

    'Funcion para borrar la reserva
    Public Sub DeleteReservation()
        Dim result As DialogResult = MessageBox.Show("¿Está seguro de que desea eliminar esta reserva?", "Confirmar eliminación", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
        If result = DialogResult.OK Then
            Dim query As String = "DELETE FROM Reserva WHERE id_reserva = @reservationId"
            Using connection As New MySqlConnection(connectionString)
                Using command As New MySqlCommand(query, connection)
                    command.Parameters.AddWithValue("@reservationId", reservationId)
                    Try
                        connection.Open()
                        Dim rowsAffected As Integer = command.ExecuteNonQuery()
                        If rowsAffected > 0 Then
                            MessageBox.Show("La reserva se ha eliminado correctamente.")
                        Else
                            MessageBox.Show("No se encontró la reserva con el ID especificado.")
                        End If
                    Catch ex As Exception
                        MessageBox.Show("Error al eliminar la reserva: " & ex.Message)
                    End Try
                End Using
            End Using
            Me.Close()
        End If
    End Sub

    'Funcion para rellenar el combobox con los productos disponibles (tabla producto)
    Private Sub FillComboBoxProducts()
        Dim query As String = "SELECT DISTINCT id_producto,nombre FROM Producto order by nombre"
        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                Try
                    connection.Open()
                    Dim reader As MySqlDataReader = command.ExecuteReader()
                    CB_Products.Items.Clear()

                    While reader.Read()
                        CB_Products.Items.Add(reader("nombre").ToString())
                        productList.Add(New Tuple(Of Integer, String)(reader("id_producto"), reader("nombre").ToString()))
                    End While

                    reader.Close()
                    connection.Close()
                Catch ex As Exception
                    MessageBox.Show("Error al editar la reserva: " & ex.Message)
                End Try
            End Using
        End Using
    End Sub

    'Funcion que toma la cantidad de producto en la bbdd y la marca en el txtbox correspondiente
    Private Sub GetProductQuantityInReservation(productName As String)
        Dim query As String = "SELECT p.nombre, pr.cantidad 
                                    FROM ProductosDeLaReserva pr 
                                    INNER JOIN Producto p ON pr.id_producto = p.id_producto 
                                    WHERE p.nombre = @selectedProduct
                                    AND pr.id_reserva = @reservationId"
        Using connection As New MySqlConnection(connectionString)
            Dim adapter As New MySqlDataAdapter(query, connection)
            adapter.SelectCommand.Parameters.AddWithValue("@selectedProduct", productName)
            adapter.SelectCommand.Parameters.AddWithValue("@reservationId", reservationId)
            Dim dataTable As New DataTable()
            adapter.Fill(dataTable)

            If dataTable.Rows.Count > 0 Then
                Dim cantidad As Integer = Convert.ToInt32(dataTable.Rows(0)("cantidad"))
                TxtBox_Quantity.Text = cantidad
            Else
                TxtBox_Quantity.Text = 0
            End If
            globalProduct = productName
        End Using
        Btn_Minus.Enabled = True
        Btn_Plus.Enabled = True
    End Sub

    'Funcion para añadir los productos seleccionados a la reserva (tabla productosdelareserva)
    Private Sub AddProductsToReservation()
        Dim prevProdsPrice As Double
        Dim result As DialogResult = MessageBox.Show("Los productos se añadirán a la reserva", "Confirmar introducción", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
        If result = DialogResult.OK Then
            Dim query As String = "INSERT INTO ProductosDeLaReserva (id_reserva, id_producto, cantidad) VALUES (@reservationId, @productId, @quantity)"
            For Each item In ListBox_ReservationProds.Items
                Using connection As New MySqlConnection(connectionString)
                    Using command As New MySqlCommand(query, connection)
                        'Funcion LINQ que da en la lista de productos el primer elemento encontrado por el nombre del producto
                        Dim element As Tuple(Of Integer, String) = productList.FirstOrDefault(Function(x) x.Item2 = item.ToString().Remove(item.ToString.Length - 2, 2))
                        Try
                            If element IsNot Nothing Then
                                Dim id As Integer = element.Item1
                                Dim nombre As String = element.Item2
                                Dim quantity = item.ToString().Remove(0, item.ToString.Length - 2)
                                'MsgBox($"Id: {id}, Nombre: {nombre}")
                                command.Parameters.AddWithValue("@reservationId", reservationId)
                                command.Parameters.AddWithValue("@productId", id)
                                command.Parameters.AddWithValue("@quantity", quantity)
                                connection.Open()
                                prevProdsPrice = SaveProductsPrice()
                                Dim rowsAffected As Integer = command.ExecuteNonQuery()
                                If rowsAffected > 0 Then
                                    Console.WriteLine($"Producto {nombre} introducido en reserva {reservationId}. Cantidad: {quantity}")
                                    ReduceInventory(id, quantity)
                                Else
                                    MessageBox.Show("No se encontró la reserva con el ID especificado.")
                                End If
                            Else
                                MessageBox.Show("No se encontró el elemento")
                            End If
                        Catch ex As Exception
                            MessageBox.Show("Error al eliminar la reserva: " & ex.Message)
                        End Try
                    End Using
                End Using
            Next
            ListBox_ReservationProds.Items.Clear()
            MessageBox.Show("Productos añadidos a la reserva.")
            CalculateReservation(prevProdsPrice)
        End If
    End Sub

    'Funcion para guardar el precio total de los productos en una variable. Devuelve el precio anterior.
    Private Function SaveProductsPrice()
        Dim prevProdsPrice = 0
        Dim query As String = "SELECT p.precio as precio, pr.cantidad as cantidad, r.precio_reserva as reserva, r.id_revision as revision FROM Producto p 
                                join productosdelareserva pr on p.id_producto = pr.id_producto 
                                join reserva r on r.id_reserva = pr.id_reserva
                                WHERE r.id_reserva = @reservationId
                                ;
                                "
        Dim prodPrice As Double
        Dim quantity As Integer
        Dim prodsTotalPrice As Double = 0

        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                command.Parameters.AddWithValue("@reservationId", reservationId)
                Try
                    connection.Open()
                    Dim reader As MySqlDataReader = command.ExecuteReader()

                    While reader.Read()
                        prodPrice = reader("precio")
                        quantity = reader("cantidad")

                        prodsTotalPrice = prodsTotalPrice + (prodPrice * quantity)
                    End While
                    prevProdsPrice = prodsTotalPrice


                    reader.Close()
                    connection.Close()
                Catch ex As Exception
                    MessageBox.Show(ex.Message)
                End Try
            End Using
        End Using
        Return prevProdsPrice
    End Function

    'Funcion para calcular el precio de la reserva 
    'Param prevProdsPrice representa el precio anterior de los productos al nuevo calculo
    Private Sub CalculateReservation(prevProdsPrice As Double)
        Dim query As String = " SELECT p.precio as precio, pr.cantidad as cantidad, r.precio_reserva as reserva, r.id_revision as revision FROM Producto p 
                                join productosdelareserva pr on p.id_producto = pr.id_producto 
                                join reserva r on r.id_reserva = pr.id_reserva
                                WHERE r.id_reserva = @reservationId
                                ;
                                "
        Dim prodPrice As Double
        Dim quantity As Integer
        Dim resPrice As Double
        Dim revId As Integer
        Dim prodsTotalPrice As Double = 0

        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                command.Parameters.AddWithValue("@reservationId", reservationId)
                Try
                    connection.Open()
                    Dim reader As MySqlDataReader = command.ExecuteReader()

                    While reader.Read()
                        prodPrice = reader("precio")
                        quantity = reader("cantidad")
                        resPrice = reader("reserva")
                        revId = reader("revision")

                        prodsTotalPrice = prodsTotalPrice + (prodPrice * quantity)
                    End While
                    If revId = 0 Then
                        resTotalPrice = resPrice + prodsTotalPrice
                        TxtBox_TotalAmount.Text = resTotalPrice
                    Else
                        resTotalPrice = resPrice + (prodsTotalPrice - prevProdsPrice)
                        TxtBox_TotalAmount.Text = resTotalPrice
                    End If


                    reader.Close()
                    connection.Close()
                Catch ex As Exception
                    MessageBox.Show(ex.Message)
                End Try
            End Using
        End Using
        UpdateRevisionId()
        UpdateReservationPrice(resTotalPrice)
    End Sub

    'Función para updatear el precio de la reserva en la bbdd
    Private Sub UpdateReservationPrice(resTotalPrice As Double)
        Dim query As String = "UPDATE Reserva SET precio_reserva = @reservationTotalPrice  WHERE id_reserva = @reservationId"
        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                command.Parameters.AddWithValue("@reservationId", reservationId)
                command.Parameters.AddWithValue("@reservationTotalPrice", resTotalPrice)
                connection.Open()
                Dim rowsAffected As Integer = command.ExecuteNonQuery()
                If rowsAffected > 0 Then
                    Console.WriteLine($"Precio total de la reserva acutalizado con éxito: {resTotalPrice}")
                Else
                    MessageBox.Show("No se encontró la reserva con el ID especificado.")
                End If
            End Using
        End Using
    End Sub

    'Funcion para actualizar el número de revision de la reserva
    Private Sub UpdateRevisionId()
        Dim updateQuery As String = "UPDATE Reserva SET id_revision = id_revision + 1 where id_reserva = @reservationId"

        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(updateQuery, connection)
                command.Parameters.AddWithValue("@reservationId", reservationId)
                Try
                    connection.Open()
                    Dim rowsAffected As Integer = command.ExecuteNonQuery()
                    If rowsAffected > 0 Then
                        Console.WriteLine("El valor de id_revision se ha actualizado correctamente.")
                    Else
                        MessageBox.Show("No se pudo actualizar el valor de id_revision.")
                    End If
                Catch ex As Exception
                    MessageBox.Show("Error al actualizar el valor de id_revision: " & ex.Message)
                End Try
            End Using
        End Using
    End Sub

    'Funcion para revisar si la reserva ya tiene factura creada
    Private Sub CheckInvoiceExistence()
        Dim invoiceExists As Boolean = False
        Dim query As String = "SELECT COUNT(*) FROM Factura WHERE id_reserva = @reservationId"

        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                command.Parameters.AddWithValue("@reservationId", reservationId)
                Try
                    connection.Open()
                    Dim result As Object = command.ExecuteScalar()

                    If result IsNot Nothing AndAlso Convert.ToInt32(result) > 0 Then
                        invoiceExists = True
                    End If
                Catch ex As Exception
                    MessageBox.Show("Error al verificar la existencia de la factura: " & ex.Message)
                End Try
            End Using
        End Using

        If invoiceExists = True Then
            Btn_EndReservation.Enabled = False
            Btn_EndReservation.Text = "RESERVA FINALIZADA"
        Else
            Btn_EndReservation.Enabled = True
            Btn_EndReservation.Text = "FINALIZAR RESERVA"
        End If
    End Sub
    'Funcion para insertar la factura en la BBDD
    Public Sub InsertInvoice()
        Dim query As String = "INSERT INTO Factura (id_reserva, total, fecha_emision) VALUES (@reservation_id, @reservationPrice, @invoiceDate)"
        Dim todayDate As Date
        todayDate = Today
        Try
            Using connection As New MySqlConnection(connectionString)
                Using command As New MySqlCommand(query, connection)
                    connection.Open()
                    command.Parameters.AddWithValue("@reservation_id", reservationId)
                    command.Parameters.AddWithValue("@invoiceDate", todayDate.ToString("yyyy-MM-dd"))
                    command.Parameters.AddWithValue("@reservationPrice", Double.Parse(TxtBox_TotalAmount.Text))

                    command.ExecuteNonQuery()
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show("Error al generar el informe: " & ex.Message)
        End Try
    End Sub

    'Reduce el inventario respecto a los productos añadidos a la reserva
    Private Sub ReduceInventory(id As Integer, quantity As Integer)
        Dim query As String = "UPDATE Inventario SET cantidad = cantidad - @quantity WHERE id_producto = @id"

        Using connection As New MySqlConnection(connectionString)
            Using command As New MySqlCommand(query, connection)
                command.Parameters.AddWithValue("@id", id)
                command.Parameters.AddWithValue("@quantity", quantity)

                Try
                    connection.Open()
                    command.ExecuteNonQuery()
                    'MsgBox("Cantidad reducida en el inventario.")
                Catch ex As Exception
                    MsgBox("Error al reducir la cantidad en el inventario: " & ex.Message)
                End Try
            End Using
        End Using
    End Sub

#End Region
End Class