using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <content>
/// The authored sample item bank used for demonstration and for the benchmark harness.
/// </content>
/// <remarks>
/// The questions are written out rather than generated, so every item carries a real stem, real
/// answers and the category it actually belongs to. Because the catalogue is fixed, a REST run and a
/// GraphQL run measure the same data and the comparison stays reproducible across machines.
/// </remarks>
public sealed partial class DatabaseSeeder
{
    private enum SampleKind
    {
        SingleResponse,
        MultipleResponse,
        EitherOr,
        Essay,
    }

    private sealed record SampleAnswer(string Text, bool IsCorrect, string? Feedback = null);

    private sealed record SampleQuestion(
        string Subject,
        string Topic,
        SampleKind Kind,
        string Stem,
        DifficultyLevel Difficulty,
        int Score,
        string[] Tags,
        SampleAnswer[]? Answers = null,
        bool AssertionIsTrue = false,
        string? Rubric = null,
        int MinimumWords = 0,
        int MaximumWords = 0,
        string? ModelAnswer = null);

    private static readonly SampleQuestion[] Questions =
    [
        new("Mathematics", "Foundations", SampleKind.SingleResponse,
            "Which number is the additive identity of the real numbers?",
            DifficultyLevel.VeryEasy, 1, ["number-theory"],
            [
                new("0", true, "Adding zero leaves any real number unchanged."),
                new("1", false, "One is the multiplicative identity, not the additive one."),
                new("-1", false),
                new("10", false),
            ]),
        new("Mathematics", "Foundations", SampleKind.MultipleResponse,
            "Which of the following numbers are prime?",
            DifficultyLevel.Easy, 2, ["number-theory"],
            [
                new("2", true, "Two is the only even prime."),
                new("17", true),
                new("21", false, "21 = 3 x 7."),
                new("51", false, "51 = 3 x 17."),
            ]),
        new("Mathematics", "Foundations", SampleKind.EitherOr,
            "Every integer is also a rational number.",
            DifficultyLevel.Easy, 1, ["number-theory"], AssertionIsTrue: true),
        new("Mathematics", "Foundations", SampleKind.Essay,
            "Explain why the square root of two cannot be written as a ratio of two integers.",
            DifficultyLevel.Hard, 5, ["number-theory"],
            Rubric: "Award marks for setting up the assumption of a fully cancelled fraction, deriving "
                + "that both numerator and denominator must be even, and naming the contradiction.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "Assume the root equals p/q in lowest terms. Squaring gives p squared equal to "
                + "two q squared, so p is even; writing p as 2k forces q to be even as well, which "
                + "contradicts the fraction being in lowest terms."),

        new("Mathematics", "Applications", SampleKind.SingleResponse,
            "A rectangle measures 8 cm by 5 cm. What is its area?",
            DifficultyLevel.VeryEasy, 1, ["geometry"],
            [
                new("40 square centimetres", true),
                new("13 square centimetres", false, "That is half the perimeter."),
                new("26 square centimetres", false, "That is the perimeter."),
                new("80 square centimetres", false),
            ]),
        new("Mathematics", "Applications", SampleKind.MultipleResponse,
            "Which expressions are equivalent to 2(x + 3)?",
            DifficultyLevel.Easy, 2, ["algebra"],
            [
                new("2x + 6", true),
                new("x + x + 6", true),
                new("2x + 3", false, "Only the first term was multiplied out."),
                new("5x", false),
            ]),
        new("Mathematics", "Applications", SampleKind.EitherOr,
            "The gradient of the line y = 3x - 4 is -4.",
            DifficultyLevel.Easy, 1, ["algebra"], AssertionIsTrue: false),
        new("Mathematics", "Applications", SampleKind.Essay,
            "A loan of 5000 euro carries 4 percent simple interest per year. Explain how to calculate "
                + "the total repayable after three years, and state one limitation of the simple "
                + "interest model.",
            DifficultyLevel.Medium, 4, ["algebra"],
            Rubric: "Award marks for the interest calculation, the total repayable, and a stated "
                + "limitation such as interest not compounding.",
            MinimumWords: 100,
            MaximumWords: 300,
            ModelAnswer: "Interest is 5000 x 0.04 x 3 = 600 euro, so 5600 euro is repayable. Simple "
                + "interest ignores compounding, so it understates the cost of most real loans."),

        new("Mathematics", "Analysis", SampleKind.SingleResponse,
            "What is the derivative of f(x) = x cubed with respect to x?",
            DifficultyLevel.Easy, 2, ["calculus"],
            [
                new("3x squared", true, "The power rule lowers the exponent by one and multiplies by it."),
                new("x squared", false),
                new("3x", false),
                new("x to the fourth over 4", false, "That is the antiderivative."),
            ]),
        new("Mathematics", "Analysis", SampleKind.MultipleResponse,
            "Which conclusions follow from the extreme value theorem for a continuous function on a "
                + "closed interval?",
            DifficultyLevel.Hard, 3, ["calculus"],
            [
                new("The function attains a maximum on the interval", true),
                new("The function attains a minimum on the interval", true),
                new("The function is differentiable on the open interval", false,
                    "Continuity does not imply differentiability."),
                new("The function is monotonic on the interval", false),
            ]),
        new("Mathematics", "Analysis", SampleKind.EitherOr,
            "If a function is differentiable at a point then it is continuous at that point.",
            DifficultyLevel.Medium, 1, ["calculus"], AssertionIsTrue: true),
        new("Mathematics", "Analysis", SampleKind.Essay,
            "Explain the difference between a sequence converging and a series converging, and give an "
                + "example that separates the two.",
            DifficultyLevel.VeryHard, 5, ["calculus"],
            Rubric: "Award marks for both definitions and for an example such as the harmonic series, "
                + "whose terms converge to zero while the series diverges.",
            MinimumWords: 150,
            MaximumWords: 400,
            ModelAnswer: "A sequence converges when its terms approach a limit; a series converges when "
                + "its partial sums do. The harmonic series has terms tending to zero yet unbounded "
                + "partial sums, so the first does not imply the second."),

        new("Mathematics", "Advanced Topics", SampleKind.SingleResponse,
            "A square matrix is invertible if and only if its determinant is:",
            DifficultyLevel.Medium, 2, ["linear-algebra"],
            [
                new("non-zero", true),
                new("zero", false, "A zero determinant means the matrix is singular."),
                new("positive", false),
                new("equal to one", false),
            ]),
        new("Mathematics", "Advanced Topics", SampleKind.MultipleResponse,
            "Which properties hold for the eigenvalues and eigenvectors of a real symmetric matrix?",
            DifficultyLevel.VeryHard, 4, ["linear-algebra"],
            [
                new("All eigenvalues are real", true),
                new("Eigenvectors for distinct eigenvalues are orthogonal", true),
                new("All eigenvalues are positive", false,
                    "That holds only for a positive definite matrix."),
                new("There is always a repeated eigenvalue", false),
            ]),
        new("Mathematics", "Advanced Topics", SampleKind.EitherOr,
            "Matrix multiplication is commutative.",
            DifficultyLevel.Medium, 1, ["linear-algebra"], AssertionIsTrue: false),
        new("Mathematics", "Advanced Topics", SampleKind.Essay,
            "Explain what the rank of a coefficient matrix tells you about the solution set of a linear "
                + "system.",
            DifficultyLevel.VeryHard, 5, ["linear-algebra"],
            Rubric: "Award marks for relating rank to consistency, for the condition on the augmented "
                + "matrix, and for the link between rank and the number of free variables.",
            MinimumWords: 150,
            MaximumWords: 400,
            ModelAnswer: "The system is consistent when the coefficient and augmented matrices share a "
                + "rank. The solution is then unique if that rank equals the number of unknowns, and "
                + "otherwise there are as many free variables as the shortfall."),

        new("Computer Science", "Foundations", SampleKind.SingleResponse,
            "How many bits are there in one byte?",
            DifficultyLevel.VeryEasy, 1, ["representation"],
            [
                new("8", true),
                new("4", false, "Four bits are a nibble."),
                new("16", false),
                new("32", false),
            ]),
        new("Computer Science", "Foundations", SampleKind.MultipleResponse,
            "Which of the following are binary representations of the decimal number 5?",
            DifficultyLevel.Easy, 2, ["representation"],
            [
                new("101", true),
                new("0101", true, "Leading zeroes do not change the value."),
                new("110", false, "That is 6."),
                new("011", false, "That is 3."),
            ]),
        new("Computer Science", "Foundations", SampleKind.EitherOr,
            "In two's complement representation the most significant bit indicates the sign of the number.",
            DifficultyLevel.Medium, 1, ["representation"], AssertionIsTrue: true),
        new("Computer Science", "Foundations", SampleKind.Essay,
            "Explain the difference between a compiler and an interpreter, and name one situation in "
                + "which each is preferable.",
            DifficultyLevel.Medium, 4, ["languages"],
            Rubric: "Award marks for the translation-ahead-of-time versus execute-as-you-go "
                + "distinction and for a defensible situation favouring each.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "A compiler translates the whole program before execution, which suits "
                + "performance-critical delivery. An interpreter executes statements as it reads them, "
                + "which suits exploratory work and short feedback loops."),

        new("Computer Science", "Applications", SampleKind.SingleResponse,
            "Which HTTP status code indicates that the request was understood but the client supplied "
                + "no valid authentication credentials?",
            DifficultyLevel.Easy, 2, ["web-apis"],
            [
                new("401 Unauthorized", true),
                new("400 Bad Request", false, "That reports a malformed request."),
                new("403 Forbidden", false,
                    "That means the caller is authenticated but not permitted."),
                new("500 Internal Server Error", false),
            ]),
        new("Computer Science", "Applications", SampleKind.MultipleResponse,
            "Which HTTP methods are defined as idempotent?",
            DifficultyLevel.Medium, 3, ["web-apis"],
            [
                new("GET", true),
                new("PUT", true),
                new("DELETE", true, "Repeating the deletion leaves the same end state."),
                new("POST", false, "Repeating a POST may create additional resources."),
            ]),
        new("Computer Science", "Applications", SampleKind.EitherOr,
            "A GraphQL query sent over HTTP POST can be cached by an intermediary in the same way as an "
                + "equivalent GET request.",
            DifficultyLevel.Hard, 2, ["web-apis"], AssertionIsTrue: false),
        new("Computer Science", "Applications", SampleKind.Essay,
            "Explain what the N+1 query problem is, how it arises in a GraphQL API, and how a batching "
                + "data loader addresses it.",
            DifficultyLevel.Hard, 5, ["web-apis", "databases"],
            Rubric: "Award marks for describing the per-parent round trip, for locating it in "
                + "field resolvers, and for explaining deferred batching by key.",
            MinimumWords: 150,
            MaximumWords: 400,
            ModelAnswer: "Resolving a field for each of N parents issues one query per parent on top of "
                + "the query that fetched them. A data loader collects the keys requested during a "
                + "tick and resolves them in a single batched query."),

        new("Computer Science", "Analysis", SampleKind.SingleResponse,
            "What is the average case time complexity of binary search on a sorted array of n elements?",
            DifficultyLevel.Easy, 2, ["complexity"],
            [
                new("O(log n)", true, "Each comparison halves the remaining range."),
                new("O(n)", false, "That is linear search."),
                new("O(n log n)", false),
                new("O(1)", false),
            ]),
        new("Computer Science", "Analysis", SampleKind.MultipleResponse,
            "Which sorting algorithms have an average case time complexity of O(n log n)?",
            DifficultyLevel.Medium, 3, ["algorithms", "complexity"],
            [
                new("Merge sort", true),
                new("Quicksort", true, "Its worst case is quadratic, but the average case is n log n."),
                new("Heapsort", true),
                new("Bubble sort", false, "Bubble sort is quadratic."),
            ]),
        new("Computer Science", "Analysis", SampleKind.EitherOr,
            "A hash table lookup has O(1) worst case time complexity.",
            DifficultyLevel.Hard, 2, ["algorithms"], AssertionIsTrue: false),
        new("Computer Science", "Analysis", SampleKind.Essay,
            "Explain the difference between time complexity and space complexity, and describe a case in "
                + "which improving one worsens the other.",
            DifficultyLevel.Medium, 4, ["complexity"],
            Rubric: "Award marks for both definitions and for a concrete trade-off such as memoisation "
                + "or a lookup table.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "Time complexity counts operations as the input grows; space complexity counts "
                + "additional memory. Memoising a recursive computation cuts repeated work but stores "
                + "every result, trading memory for speed."),

        new("Computer Science", "Advanced Topics", SampleKind.SingleResponse,
            "In the ACID properties of a database transaction, what does the letter I stand for?",
            DifficultyLevel.Easy, 2, ["databases"],
            [
                new("Isolation", true),
                new("Integrity", false),
                new("Indexing", false),
                new("Idempotence", false),
            ]),
        new("Computer Science", "Advanced Topics", SampleKind.MultipleResponse,
            "Which phenomena are transaction isolation levels designed to prevent?",
            DifficultyLevel.Hard, 3, ["databases"],
            [
                new("Dirty reads", true),
                new("Non-repeatable reads", true),
                new("Phantom reads", true),
                new("Deadlocks", false,
                    "Deadlocks are resolved by the lock manager, not by the isolation level."),
            ]),
        new("Computer Science", "Advanced Topics", SampleKind.EitherOr,
            "Under the CAP theorem a distributed system can guarantee consistency, availability and "
                + "partition tolerance at the same time.",
            DifficultyLevel.Hard, 2, ["distributed-systems"], AssertionIsTrue: false),
        new("Computer Science", "Advanced Topics", SampleKind.Essay,
            "Compare optimistic and pessimistic concurrency control, and justify a choice between them "
                + "for a collaborative document editor.",
            DifficultyLevel.VeryHard, 5, ["databases", "distributed-systems"],
            Rubric: "Award marks for both mechanisms, for the conflict-rate argument, and for a "
                + "justified recommendation.",
            MinimumWords: 150,
            MaximumWords: 400,
            ModelAnswer: "Pessimistic control locks records for the duration of the edit; optimistic "
                + "control detects a conflicting version at write time. A collaborative editor has many "
                + "concurrent readers and rare true conflicts, so optimistic control keeps throughput up."),

        new("Physics", "Foundations", SampleKind.SingleResponse,
            "What is the SI unit of force?",
            DifficultyLevel.VeryEasy, 1, ["units"],
            [
                new("Newton", true),
                new("Joule", false, "The joule is the unit of energy."),
                new("Watt", false, "The watt is the unit of power."),
                new("Pascal", false, "The pascal is the unit of pressure."),
            ]),
        new("Physics", "Foundations", SampleKind.MultipleResponse,
            "Which of the following are SI base units?",
            DifficultyLevel.Easy, 2, ["units"],
            [
                new("Metre", true),
                new("Kilogram", true),
                new("Second", true),
                new("Newton", false, "The newton is a derived unit."),
            ]),
        new("Physics", "Foundations", SampleKind.EitherOr,
            "Mass and weight are the same physical quantity.",
            DifficultyLevel.Easy, 1, ["mechanics"], AssertionIsTrue: false),
        new("Physics", "Foundations", SampleKind.Essay,
            "Explain the difference between scalar and vector quantities and give two examples of each.",
            DifficultyLevel.Easy, 3, ["mechanics"],
            Rubric: "Award marks for the magnitude-only versus magnitude-and-direction distinction and "
                + "for two correct examples of each kind.",
            MinimumWords: 100,
            MaximumWords: 300,
            ModelAnswer: "A scalar has magnitude alone, such as mass or temperature. A vector also has "
                + "direction, such as velocity or force, so vectors must be added geometrically."),

        new("Physics", "Applications", SampleKind.SingleResponse,
            "A car accelerates uniformly from rest to 20 metres per second in 5 seconds. What is its "
                + "acceleration?",
            DifficultyLevel.Easy, 2, ["mechanics"],
            [
                new("4 metres per second squared", true),
                new("0.25 metres per second squared", false, "That inverts the calculation."),
                new("15 metres per second squared", false),
                new("100 metres per second squared", false, "That multiplies instead of dividing."),
            ]),
        new("Physics", "Applications", SampleKind.MultipleResponse,
            "Which forces act on a book resting on a horizontal table?",
            DifficultyLevel.Easy, 2, ["mechanics"],
            [
                new("Its weight", true),
                new("The normal reaction from the table", true),
                new("A horizontal applied force", false, "Nothing is pushing the book sideways."),
                new("Air resistance", false, "The book is not moving through the air."),
            ]),
        new("Physics", "Applications", SampleKind.EitherOr,
            "A body moving at constant velocity has zero net force acting on it.",
            DifficultyLevel.Medium, 1, ["mechanics"], AssertionIsTrue: true),
        new("Physics", "Applications", SampleKind.Essay,
            "A ball is dropped from a height of 20 metres. Explain how to find the time it takes to "
                + "reach the ground and state the assumptions you make.",
            DifficultyLevel.Medium, 4, ["mechanics"],
            Rubric: "Award marks for selecting the correct equation of motion, for the numerical "
                + "answer, and for stating assumptions such as negligible air resistance.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "With the ball starting at rest, the distance equals half g t squared, so t is "
                + "about 2.0 seconds. This assumes negligible air resistance and a constant g."),

        new("Physics", "Analysis", SampleKind.SingleResponse,
            "Which quantity is conserved in an elastic collision but not in an inelastic collision?",
            DifficultyLevel.Medium, 2, ["mechanics"],
            [
                new("Kinetic energy", true),
                new("Momentum", false, "Momentum is conserved in both."),
                new("Mass", false),
                new("Charge", false),
            ]),
        new("Physics", "Analysis", SampleKind.MultipleResponse,
            "Which statements follow from the first law of thermodynamics?",
            DifficultyLevel.Hard, 3, ["thermodynamics"],
            [
                new("Energy is conserved, being neither created nor destroyed", true),
                new("The change in internal energy equals the heat added minus the work done by the system",
                    true),
                new("The entropy of an isolated system never decreases", false,
                    "That is the second law."),
                new("Heat flows spontaneously from a colder to a hotter body", false),
            ]),
        new("Physics", "Analysis", SampleKind.EitherOr,
            "Momentum is conserved in every collision within a closed system, whether it is elastic or "
                + "inelastic.",
            DifficultyLevel.Medium, 1, ["mechanics"], AssertionIsTrue: true),
        new("Physics", "Analysis", SampleKind.Essay,
            "Explain why a satellite in a circular orbit is accelerating even though its speed is "
                + "constant.",
            DifficultyLevel.Hard, 4, ["mechanics"],
            Rubric: "Award marks for treating velocity as a vector, for identifying the centripetal "
                + "direction, and for naming gravity as the centripetal force.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "Acceleration is the rate of change of velocity, and velocity includes "
                + "direction. The satellite's direction changes continuously, and the gravitational "
                + "pull towards the centre supplies that centripetal acceleration."),

        new("Physics", "Advanced Topics", SampleKind.SingleResponse,
            "In special relativity, which quantity is the same in every inertial reference frame?",
            DifficultyLevel.Hard, 3, ["relativity"],
            [
                new("The speed of light in a vacuum", true),
                new("The time interval between two events", false, "Time intervals dilate."),
                new("The length of a moving rod", false, "Lengths contract along the direction of motion."),
                new("The kinetic energy of a particle", false),
            ]),
        new("Physics", "Advanced Topics", SampleKind.MultipleResponse,
            "Which phenomena provide evidence for the wave nature of light?",
            DifficultyLevel.Hard, 3, ["waves"],
            [
                new("Diffraction", true),
                new("Interference", true),
                new("Polarisation", true),
                new("The photoelectric effect", false, "That is evidence for the particle nature."),
            ]),
        new("Physics", "Advanced Topics", SampleKind.EitherOr,
            "The Heisenberg uncertainty principle allows the position and the momentum of a particle to "
                + "be known exactly at the same instant.",
            DifficultyLevel.Hard, 2, ["quantum"], AssertionIsTrue: false),
        new("Physics", "Advanced Topics", SampleKind.Essay,
            "Explain time dilation in special relativity and describe one experimental observation that "
                + "supports it.",
            DifficultyLevel.VeryHard, 5, ["relativity"],
            Rubric: "Award marks for the relationship between relative speed and elapsed time and for a "
                + "genuine observation such as atmospheric muon lifetimes.",
            MinimumWords: 150,
            MaximumWords: 400,
            ModelAnswer: "A clock moving relative to an observer is measured to tick more slowly, by the "
                + "Lorentz factor. Muons created high in the atmosphere reach the ground in far greater "
                + "numbers than their rest lifetime would allow, which matches the prediction."),

        new("Statistics", "Foundations", SampleKind.SingleResponse,
            "Which measure of central tendency is most affected by a single extreme outlier?",
            DifficultyLevel.Easy, 2, ["descriptive-statistics"],
            [
                new("The mean", true, "Every observation enters the mean with equal weight."),
                new("The median", false, "The median depends only on the middle of the ordered data."),
                new("The mode", false),
                new("The interquartile range", false, "That is a measure of spread."),
            ]),
        new("Statistics", "Foundations", SampleKind.MultipleResponse,
            "Which of the following are measures of dispersion?",
            DifficultyLevel.Easy, 2, ["descriptive-statistics"],
            [
                new("Variance", true),
                new("Standard deviation", true),
                new("Interquartile range", true),
                new("Median", false, "The median is a measure of central tendency."),
            ]),
        new("Statistics", "Foundations", SampleKind.EitherOr,
            "The median of a data set is always one of the observed values.",
            DifficultyLevel.Medium, 1, ["descriptive-statistics"], AssertionIsTrue: false),
        new("Statistics", "Foundations", SampleKind.Essay,
            "Explain the difference between a population and a sample, and why a sample statistic is "
                + "treated as an estimate.",
            DifficultyLevel.Easy, 3, ["sampling"],
            Rubric: "Award marks for both definitions and for the idea that a statistic varies from "
                + "sample to sample.",
            MinimumWords: 100,
            MaximumWords: 300,
            ModelAnswer: "A population is every unit of interest; a sample is the subset actually "
                + "measured. Because a different sample would give a different value, the statistic "
                + "estimates the population parameter rather than reporting it."),

        new("Statistics", "Applications", SampleKind.SingleResponse,
            "A fair six-sided die is rolled once. What is the probability of rolling a number greater "
                + "than four?",
            DifficultyLevel.Easy, 2, ["probability"],
            [
                new("One third", true, "Two of the six faces qualify."),
                new("One sixth", false, "That counts only one face."),
                new("One half", false, "That counts three faces."),
                new("Two thirds", false),
            ]),
        new("Statistics", "Applications", SampleKind.MultipleResponse,
            "Which sampling methods are forms of probability sampling?",
            DifficultyLevel.Medium, 3, ["sampling"],
            [
                new("Simple random sampling", true),
                new("Stratified sampling", true),
                new("Systematic sampling", true),
                new("Convenience sampling", false,
                    "Selection probabilities are unknown, so it is non-probability sampling."),
            ]),
        new("Statistics", "Applications", SampleKind.EitherOr,
            "Two events are independent when the occurrence of one does not change the probability of "
                + "the other.",
            DifficultyLevel.Easy, 1, ["probability"], AssertionIsTrue: true),
        new("Statistics", "Applications", SampleKind.Essay,
            "A survey is run by inviting visitors to a university website to respond. Explain what bias "
                + "this introduces and how it affects conclusions about the whole student population.",
            DifficultyLevel.Medium, 4, ["sampling"],
            Rubric: "Award marks for naming self-selection or coverage bias, for who is "
                + "over-represented, and for the effect on generalisability.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "Only students who visit the site and choose to answer are included, which "
                + "over-represents the engaged and the online. The results describe the respondents "
                + "rather than the student body, so they cannot be generalised."),

        new("Statistics", "Analysis", SampleKind.SingleResponse,
            "A hypothesis test at the 5 percent significance level returns a p-value of 0.03. What is "
                + "the correct conclusion?",
            DifficultyLevel.Medium, 3, ["inference"],
            [
                new("There is sufficient evidence to reject the null hypothesis", true),
                new("The null hypothesis is true with probability 0.03", false,
                    "The p-value is not the probability that the null hypothesis is true."),
                new("The alternative hypothesis is true with probability 0.97", false),
                new("The result cannot be significant at any level", false),
            ]),
        new("Statistics", "Analysis", SampleKind.MultipleResponse,
            "Which statements about a 95 percent confidence interval are correct?",
            DifficultyLevel.Hard, 3, ["inference"],
            [
                new("Over repeated sampling, 95 percent of such intervals contain the parameter", true),
                new("A wider interval reflects greater uncertainty", true),
                new("There is a 95 percent probability that the parameter lies in this interval", false,
                    "The parameter is fixed; the interval is what varies."),
                new("It is guaranteed to contain the mean of a future sample", false),
            ]),
        new("Statistics", "Analysis", SampleKind.EitherOr,
            "A statistically significant result is necessarily of practical importance.",
            DifficultyLevel.Medium, 1, ["inference"], AssertionIsTrue: false),
        new("Statistics", "Analysis", SampleKind.Essay,
            "Explain the difference between a Type I and a Type II error and describe the trade-off "
                + "between them.",
            DifficultyLevel.Hard, 4, ["inference"],
            Rubric: "Award marks for both definitions and for the effect of moving the significance "
                + "level on each error rate.",
            MinimumWords: 120,
            MaximumWords: 350,
            ModelAnswer: "A Type I error rejects a true null hypothesis; a Type II error fails to reject "
                + "a false one. Lowering the significance level reduces the first and increases the "
                + "second unless the sample size grows."),

        new("Statistics", "Advanced Topics", SampleKind.SingleResponse,
            "In a simple linear regression, what does the coefficient of determination R squared "
                + "measure?",
            DifficultyLevel.Medium, 3, ["regression"],
            [
                new("The proportion of variance in the response explained by the model", true),
                new("The slope of the fitted line", false),
                new("The correlation between the residuals", false),
                new("The standard error of the estimate", false),
            ]),
        new("Statistics", "Advanced Topics", SampleKind.MultipleResponse,
            "Which assumptions underlie ordinary least squares regression?",
            DifficultyLevel.VeryHard, 4, ["regression"],
            [
                new("The errors have constant variance", true),
                new("The errors are independent of one another", true),
                new("The model is linear in its parameters", true),
                new("The response variable is normally distributed before fitting", false,
                    "The assumption concerns the errors, not the raw response."),
            ]),
        new("Statistics", "Advanced Topics", SampleKind.EitherOr,
            "A high correlation between two variables establishes that one of them causes the other.",
            DifficultyLevel.Easy, 1, ["regression"], AssertionIsTrue: false),
        new("Statistics", "Advanced Topics", SampleKind.Essay,
            "Explain what multicollinearity is in a multiple regression model, how it can be detected, "
                + "and why it complicates the interpretation of the coefficients.",
            DifficultyLevel.VeryHard, 5, ["regression"],
            Rubric: "Award marks for the definition, for a detection method such as the variance "
                + "inflation factor, and for the effect on standard errors and interpretation.",
            MinimumWords: 150,
            MaximumWords: 400,
            ModelAnswer: "Multicollinearity is strong linear association among predictors. It shows up "
                + "in high variance inflation factors and inflates the standard errors, so individual "
                + "coefficients become unstable and cannot be read as separate effects."),
    ];

    private async Task SeedSampleContentAsync(CancellationToken cancellationToken)
    {
        if (await context.Items.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var administratorEmail = EmailAddress.Create(_options.AdministratorEmail).Normalized;
        var author = await context.Users
            .FirstAsync(user => user.Email.Normalized == administratorEmail, cancellationToken);

        var categories = await SeedCategoriesAsync(cancellationToken);
        var tags = await SeedTagsAsync(cancellationToken);

        for (var index = 0; index < _options.SampleItemCount; index++)
        {
            // A bank larger than the catalogue repeats it, which is what the benchmark run needs:
            // volume without inventing questions nobody wrote.
            var question = Questions[index % Questions.Length];
            var item = CreateItem(question, categories[(question.Subject, question.Topic)], author.Id);
            item.ReplaceTags(question.Tags.Select(tag => tags[tag]).ToList());
            PromoteToPublished(item, index);
            context.Items.Add(item);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded {Count} sample items from {Distinct} authored questions.",
            _options.SampleItemCount,
            Math.Min(_options.SampleItemCount, Questions.Length));
    }

    private async Task<Dictionary<(string Subject, string Topic), CategoryId>> SeedCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var leaves = new Dictionary<(string, string), CategoryId>();

        foreach (var subjectName in Questions.Select(question => question.Subject).Distinct())
        {
            var subject = Category.Create(
                CategoryName.Create(subjectName),
                $"Assessment content for {subjectName}.");
            context.Categories.Add(subject);

            var topicNames = Questions
                .Where(question => question.Subject == subjectName)
                .Select(question => question.Topic)
                .Distinct();

            foreach (var topicName in topicNames)
            {
                var topic = Category.Create(
                    CategoryName.Create(topicName),
                    $"{topicName} within {subjectName}.",
                    subject.Id);
                context.Categories.Add(topic);
                leaves.Add((subjectName, topicName), topic.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return leaves;
    }

    private async Task<Dictionary<string, TagId>> SeedTagsAsync(CancellationToken cancellationToken)
    {
        var labels = Questions.SelectMany(question => question.Tags).Distinct().Order().ToList();
        var tags = labels.ToDictionary(label => label, label => Tag.Create(TagName.Create(label)));
        context.Tags.AddRange(tags.Values);
        await context.SaveChangesAsync(cancellationToken);
        return tags.ToDictionary(entry => entry.Key, entry => entry.Value.Id);
    }

    private static Item CreateItem(SampleQuestion question, CategoryId category, UserId author)
    {
        var stem = ItemStem.Create(question.Stem);
        var score = Points.Create(question.Score);

        return question.Kind switch
        {
            SampleKind.SingleResponse => SingleResponseItem.Create(
                stem, question.Difficulty, category, score, author, ToOptions(question.Answers!)),

            SampleKind.MultipleResponse => MultipleResponseItem.Create(
                stem, question.Difficulty, category, score, author, ToOptions(question.Answers!)),

            SampleKind.EitherOr => EitherOrItem.Create(
                stem, question.Difficulty, category, score, author, "True", "False",
                question.AssertionIsTrue),

            _ => EssayItem.Create(
                stem,
                question.Difficulty,
                category,
                score,
                author,
                EssayRubric.Create(question.Rubric, question.MinimumWords, question.MaximumWords),
                question.ModelAnswer),
        };
    }

    private static List<ItemOption> ToOptions(SampleAnswer[] answers)
        => [.. answers.Select((answer, position) =>
            ItemOption.Create(answer.Text, answer.IsCorrect, position, answer.Feedback))];

    private static void PromoteToPublished(Item item, int index)
    {
        // Roughly three quarters of the bank is published so the exam builder has material to work
        // with, while the remainder exercises every other lifecycle state.
        switch (index % 8)
        {
            case 0:
                return;

            case 1:
                item.SubmitForReview();
                return;

            case 2:
                item.SubmitForReview();
                item.Approve();
                return;

            default:
                item.SubmitForReview();
                item.Approve();
                item.Publish(DateTimeOffset.UtcNow);
                return;
        }
    }
}
