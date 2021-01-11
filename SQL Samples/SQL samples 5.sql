CREATE FUNCTION [dbo].[fn_interactionreasons]
(
	@IdInteraction INT
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @Reasons NVARCHAR(MAX)
	SET @Reasons = ''

	-- Add the T-SQL statements to compute the return value here
	SELECT @Reasons = @Reasons + ', ' + A.[Description] FROM [dbo].[Reason] A
	JOIN [dbo].[Interaction_Reason] B ON B.[IdReason] = A.[Id]
	WHERE B.[IdInteraction] = @IdInteraction

	-- Return the result of the function
	RETURN SUBSTRING(@Reasons, 3, 4000)
END
GO
/****** Object:  UserDefinedFunction [dbo].[fn_parseaccountnumber]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
