CREATE FUNCTION [dbo].[fn_estimateservicetime]
(
	@IdInteraction INT
)
RETURNS TIME
AS
BEGIN
	-- Declare the return variable here
	DECLARE @InteractionReasons NVARCHAR(4000) = [dbo].[fn_interactionreasonsid](@IdInteraction)

	DECLARE @Result TIME
	SET @Result = '00:00:00'

	-- Check if there is a combination
	DECLARE @CombinationAverageTime TIME = '00:00:00'
	
	SELECT @CombinationAverageTime = [AverageServiceTime] FROM [dbo].[ReasonCombination]
	WHERE [Description] = @InteractionReasons

	IF @CombinationAverageTime IS NOT NULL AND @CombinationAverageTime <> '00:00:00'
		BEGIN
			RETURN @CombinationAverageTime
		END

	-- ELSE use the Reason table (AverageServiceTime column)
	DECLARE @Reasons TABLE (Id int)
	INSERT INTO @Reasons
	SELECT [IdReason] FROM [dbo].[Interaction_Reason]
	WHERE [IdInteraction] = @IdInteraction

	SELECT @Result = [dbo].[fn_timesum](@Result, [AverageServiceTime]) FROM [dbo].[Reason]
	WHERE [Id] IN (SELECT * FROM @Reasons)

	-- Return the result of the function
	RETURN @Result
END
GO