using System;

namespace MopBot.Common.Systems.Trivia
{
	[Serializable]
	public class TriviaQuestion
	{
		public string question;
		public string[] answers;
		public bool wasPosted;

		public TriviaQuestion(string question,string[] answers)
		{
			this.question = question;
			this.answers = answers;
		}
	}
}
