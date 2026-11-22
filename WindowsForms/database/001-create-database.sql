/* Ejecutar una sola vez en SQL Server Management Studio con permisos de creación. */
IF DB_ID(N'NEXUS') IS NULL
    CREATE DATABASE [NEXUS];
GO
USE [NEXUS];
GO
/* Después ejecutar 002-schema.sql. La interfaz también puede crear la base y aplicar el esquema. */
