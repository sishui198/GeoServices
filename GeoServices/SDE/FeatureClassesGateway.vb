﻿Imports ESRI.ArcGIS.Geodatabase
Imports System.Xml

Namespace SDE
    ''' <summary>
    ''' Permite obtener FeatureClasses y Tablas de una base de datos SDE
    ''' </summary>
    ''' <remarks>
    ''' Los esquemas de obtención de datos están adaptados siguiente formato:
    '''     SDE -> FeatureClasses (muchos),
    '''     SDE -> FeatureDatasets (muchos) -> FeatureClasses (muchos)
    ''' 
    ''' Si se desea alterar, debe crease una nueva clase que herede de XMLGetter.
    ''' En caso de querer el mismo esquema, pero requerir una validación diferente para los FeatureClass o Tablas
    ''' se debe crear una clase que herede de esta y sobreescribir el método isValid realizando la validación correspondiente 
    ''' </remarks>
    Public Class FeatureClassesGateway
        Inherits SDEDataGateway(Of IFeatureClass)

        ''' <summary>
        ''' Retorna el siguiente Feature Class
        ''' </summary>
        Protected Function getNextFClass(ByRef subset As IEnumDataset, ByVal dataset As IDataset) As IFeatureClass
            While Not isFClass(dataset)
                dataset = subset.Next
                If dataset Is Nothing Then Return Nothing
            End While
            Return dataset
        End Function

        ''' <summary>
        ''' Retorna si el Dataset ingresado es Feature Class
        ''' </summary>
        Protected Function isFClass(ByVal dataset As IDataset) As Boolean
            Return TypeOf dataset Is IFeatureClass OrElse dataset Is Nothing
        End Function

        ''' <summary>
        ''' Realiza la validación de un dataset (Feature Class, Tabla, Feature Dataset...) según lo especificado
        ''' </summary>
        ''' <remarks>
        ''' La validación realizada acá es que sea una capa en producción y no sea de red.
        ''' Para realizar otra validación se debe generar una subclase que sobreescriba este método
        ''' </remarks>
        Protected Overridable Function validDataset(ByVal dataset As IDataset) As Boolean
            Return Not Me.SanitizeString(dataset.Name).ToUpper Like "*AUDITORIA*" AndAlso Not Me.SanitizeString(dataset.Name).ToUpper Like "*HISTORICO*" AndAlso Not Me.SanitizeString(dataset.Name).ToUpper Like "*RED*" AndAlso TypeOf dataset Is IFeatureClass
        End Function

        ''' <summary>
        ''' Realiza la validación de un dataset (Feature Class, Tabla, Feature Dataset...) según lo especificado
        ''' </summary>
        ''' <remarks>
        ''' La validación realizada acá es que sea una capa en producción y no sea de red.
        ''' Para realizar otra validación se debe generar una subclase que sobreescriba este método
        ''' </remarks>
        Protected Overridable Function validFeatureClass(ByVal fclass As IFeatureClass) As Boolean
            Return Not Me.SanitizeString(fclass.AliasName).ToUpper Like "*AUDITORIA*" AndAlso Not Me.SanitizeString(fclass.AliasName).ToUpper Like "*HISTORICO*" AndAlso Not Me.SanitizeString(fclass.AliasName).ToUpper Like "*RED*"
        End Function

        ''' <summary>
        ''' Realiza la validación de un dataset (Feature Class, Tabla, Feature Dataset...) según lo especificado
        ''' </summary>
        ''' <remarks>
        ''' La validación realizada acá es que sea una capa en producción y no sea de red.
        ''' Para realizar otra validación se debe generar una subclase que sobreescriba este método
        ''' </remarks>
        Protected Overridable Function isValid(ByVal dataset As IDataset, Optional ByVal Privileges As SDE.SDEPrivileges = SDEPrivileges.SDEEdit) As Boolean
            Return Me.validFeatureClass(dataset) AndAlso Me.validDataset(dataset) AndAlso Me.PermissionsValidation(dataset, Privileges)
        End Function

        ''' <summary>
        ''' Obtiene una lista de todos los FeatureClasses para la conexión especificada
        ''' </summary>
        Protected Overrides Function doGetAll(ByVal workspace As IWorkspace, ByVal Privileges As SDE.SDEPrivileges) As System.Collections.Generic.List(Of ESRI.ArcGIS.Geodatabase.IFeatureClass)
            Dim fclasses As New List(Of IFeatureClass)

            Dim datasets As IEnumDataset = workspace.Datasets(esriDatasetType.esriDTFeatureDataset)
            Dim featureClasses As IEnumDataset = workspace.Datasets(esriDatasetType.esriDTFeatureClass)

            Dim fclass As IFeatureClass = featureClasses.Next

            While Not fclass Is Nothing
                If Me.isValid(fclass, Privileges) Then fclasses.Add(fclass)
                fclass = featureClasses.Next
            End While

            System.Runtime.InteropServices.Marshal.ReleaseComObject(featureClasses)

            Dim dataset As IFeatureDataset = datasets.Next
            While Not dataset Is Nothing
                If TypeOf fclass Is IFeatureClass AndAlso Me.isValid(fclass, Privileges) Then fclasses.Add(dataset)
                If Not dataset.Subsets Is Nothing Then
                    Dim subset As IEnumDataset = dataset.Subsets
                    fclass = Me.getNextFClass(subset, subset.Next)
                    While Not fclass Is Nothing
                        If Me.isValid(fclass, Privileges) Then fclasses.Add(fclass)
                        fclass = Me.getNextFClass(subset, subset.Next)
                    End While
                End If
                dataset = datasets.Next
            End While

            System.Runtime.InteropServices.Marshal.ReleaseComObject(datasets)

            Return fclasses
        End Function

        Protected Function ReturnSingleElement(ByVal name As String, ByVal fclass As IFeatureClass, ByVal Privileges As SDEPrivileges)
            If Me.IsNameEquals(fclass, name) Then
                If Me.PermissionsValidation(fclass, Privileges) Then
                    Return fclass
                Else
                    Throw New DataException("El FeatureClass " & name & " no puede ser abierto para edición")
                End If
            Else : Return Nothing
            End If
        End Function

        Protected Overrides Function IsNameEquals(ByVal element As ESRI.ArcGIS.Geodatabase.IFeatureClass, ByVal name As String) As Boolean
            Return CType(element, IDataset).Name.ToUpper().Contains(name.ToUpper()) OrElse element.AliasName.ToUpper().Contains(name.ToUpper())
        End Function

        Protected Overrides Function doGetByName(ByVal name As String, ByVal workspace As ESRI.ArcGIS.Geodatabase.IFeatureWorkspace) As ESRI.ArcGIS.Geodatabase.IFeatureClass
            Return workspace.OpenFeatureClass(name)
        End Function
    End Class
End Namespace