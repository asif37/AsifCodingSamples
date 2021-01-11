CREATE PROCEDURE [dbo].[sp_addbreaktime]
	@IdLocation INT,
	@IdEmployee INT,
	@DayOfWeek INT,
	@NextAvailableTime TIME
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @Time TIME = @NextAvailableTime

	DECLARE @LocationEndTime TIME 
	DECLARE @ScheduleEndTime TIME = '00:00:00'
	DECLARE @IdSchedule INT

	SELECT @LocationEndTime = CASE
								WHEN @DayOfWeek = 0 AND [SundayEnd] IS NOT NULL THEN [SundayEnd]
								WHEN @DayOfWeek = 1 AND [MondayEnd] IS NOT NULL THEN [MondayEnd]
								WHEN @DayOfWeek = 2 AND [TuesdayEnd] IS NOT NULL THEN [TuesdayEnd]
								WHEN @DayOfWeek = 3 AND [WednesdayEnd] IS NOT NULL THEN [WednesdayEnd]
								WHEN @DayOfWeek = 4 AND [ThursdayEnd] IS NOT NULL THEN [ThursdayEnd]
								WHEN @DayOfWeek = 5 AND [FridayEnd] IS NOT NULL THEN [FridayEnd]
								WHEN @DayOfWeek = 6 AND [SaturdayEnd] IS NOT NULL THEN [SaturdayEnd]
								ELSE '00:00:00'
							  END
	FROM [dbo].[Location] 
	WHERE [Id] = @IdLocation

	SELECT @ScheduleEndTime = [EndTime], @IdSchedule = [Id]
	FROM [dbo].[ActiveSchedule]
	WHERE [IdEmployee] = @IdEmployee

	DECLARE @BreakEnd TIME = '00:00:00'

	SELECT @BreakEnd = [EndTime] FROM [dbo].[ActiveSchedule_Events]
	WHERE [IdSchedule] = @IdSchedule
	AND [dbo].[fn_timediff]('00:05:00', [StartTime]) <= @NextAvailableTime
	AND [EndTime] > @NextAvailableTime

	IF @BreakEnd IS NOT NULL AND @BreakEnd <> '00:00:00'
		BEGIN
			SET @Time = @BreakEnd
		END

	IF @LocationEndTime <> '00:00:00' AND @ScheduleEndTime IS NOT NULL AND @ScheduleEndTime <> '00:00:00'
		BEGIN
			IF @ScheduleEndTime < @LocationEndTime AND @ScheduleEndTime <= @Time
				BEGIN
					SET @Time = '00:00:00'
				END
		END
	ELSE IF @SCheduleEndTime IS NOT NULL AND @ScheduleEndTime <> '00:00:00' AND @ScheduleEndTime <= @Time
		BEGIN
			SET @Time = '00:00:00'
		END

	SELECT @Time AS NextTime
END
GO