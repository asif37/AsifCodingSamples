CREATE FUNCTION [dbo].[fn_parseaccountnumber]
(
	@AccountNumber NVARCHAR(10)
)
RETURNS NVARCHAR(10)
AS
BEGIN
	DECLARE @IntAlpha INT
	SET @IntAlpha = PATINDEX('%[^0-9]%', @AccountNumber)

	WHILE @IntAlpha > 0
		BEGIN
			SET @AccountNumber = STUFF(@AccountNumber, @IntAlpha, 1, '')
			SET @IntAlpha = PATINDEX('%[^0-9]%', @AccountNumber)
		END

	RETURN ISNULL(@AccountNumber, '')
END
GO
/****** Object:  UserDefinedFunction [dbo].[fn_timediff]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO