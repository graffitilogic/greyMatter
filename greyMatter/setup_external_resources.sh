#!/bin/bash

# External Language Resources Setup
# Downloads corpus-based language data for realistic training

echo "🌐 Setting up external language resources..."

# Create resources directory
mkdir -p external_resources
cd external_resources

# 1. Download Google Books 1-gram frequency data (sample)
echo "📚 Downloading word frequency data..."
if [ ! -f "google_books_1grams_sample.txt" ]; then
    # Download a curated sample of most frequent words
    curl -o "frequency_lists.zip" "https://www.wordfrequency.info/files/entries_1_2.zip" 2>/dev/null || true
    if [ -f "frequency_lists.zip" ]; then
        unzip -q frequency_lists.zip
        echo "✅ Word frequency data downloaded"
    else
        echo "⚠️  Could not download frequency data, creating fallback list"
        cat > "top_5000_words.txt" << 'EOF'
the,22038615,Art
be,12545825,Verb
of,10741073,Prep
and,10343885,Conj
a,10144200,Art
to,9038375,Prep
in,6801323,Prep
he,5077636,Pron
have,4637489,Verb
it,4196690,Pron
that,3825331,Pron/Conj
for,3694874,Prep
they,2893193,Pron
I,2887039,Pron
with,2687963,Prep
as,2673398,Prep/Conj
not,2558841,Adv
on,2482412,Prep
she,2329260,Pron
at,2256863,Prep
by,1761242,Prep
this,1717363,Pron
we,1660481,Pron
you,1629681,Pron
do,1483515,Verb
but,1445618,Conj
from,1384992,Prep
or,1340473,Conj
which,1294796,Pron
one,1242441,Num
would,1239950,Verb
all,1179307,Pron
will,1115334,Verb
there,1096710,Adv
say,1056414,Verb
who,1028966,Pron
make,1004985,Verb
when,986463,Adv/Conj
can,976384,Verb
more,953104,Adv
if,925515,Conj
no,897865,Art
man,885524,Noun
out,874715,Adv/Prep
other,860289,Adj
so,839887,Adv
what,837625,Pron
time,820976,Noun
up,816060,Adv
go,810123,Verb
about,806001,Prep
than,790324,Prep/Conj
into,784576,Prep
could,776865,Verb
state,776806,Noun
only,771074,Adv
new,764914,Adj
year,759629,Noun
some,745252,Pron
take,742982,Verb
come,731027,Verb
these,721832,Pron
know,712904,Verb
see,706012,Verb
use,705141,Verb
get,696151,Verb
like,689124,Prep/Verb
then,688020,Adv
first,686893,Adj/Adv
any,674499,Pron
work,668624,Noun/Verb
now,665404,Adv
may,659738,Verb
such,656177,Adj
give,652644,Verb
over,651314,Prep/Adv
think,649731,Verb
most,634594,Adj/Adv
even,630732,Adv
find,630468,Verb
day,629742,Noun
also,626427,Adv
after,625092,Prep/Adv
way,612317,Noun
many,609015,Adj
must,607515,Verb
look,602734,Verb
before,598984,Prep/Adv
great,595006,Adj
back,594577,Adv/Noun
through,589889,Prep/Adv
long,586082,Adj
where,582622,Adv
much,580757,Adv/Adj
should,580015,Verb
well,569686,Adv/Adj
people,567915,Noun
down,566808,Adv/Prep
own,566532,Adj
just,563825,Adv
because,561464,Conj
good,560698,Adj
each,555788,Pron
those,549993,Pron
feel,548999,Verb
seem,547545,Verb
how,544264,Adv
high,540085,Adj
too,537128,Adv
place,534628,Noun
little,533140,Adj
world,529014,Noun
very,527332,Adv
still,523807,Adv
nation,518892,Noun
hand,518203,Noun
old,517456,Adj
life,516194,Noun
tell,513983,Verb
write,509761,Verb
become,507275,Verb
here,505883,Adv
show,504784,Verb
house,502717,Noun
both,501441,Pron
between,500692,Prep
need,499764,Verb
mean,497712,Verb
call,497456,Verb
develop,495647,Verb
under,495009,Prep
last,494727,Adj/Adv
right,493749,Adj/Adv
move,492874,Verb
thing,491817,Noun
general,490752,Adj
school,489506,Noun
never,489019,Adv
same,488545,Adj
another,487848,Pron
begin,486886,Verb
while,483472,Conj
number,481977,Noun
part,481515,Noun
turn,480053,Verb
real,479176,Adj
leave,476820,Verb
might,476515,Verb
want,476142,Verb
point,475635,Noun
form,474702,Noun/Verb
off,474138,Adv/Prep
child,473593,Noun
few,472445,Adj
small,470967,Adj
since,470760,Prep/Adv/Conj
against,469744,Prep
ask,467364,Verb
late,467274,Adj/Adv
home,466986,Noun/Adv
interest,466376,Noun
large,465131,Adj
person,464647,Noun
end,463559,Noun/Verb
open,463116,Verb/Adj
public,462632,Adj
follow,462107,Verb
during,460726,Prep
present,459819,Adj/Verb
without,459648,Prep
again,458843,Adv
hold,458433,Verb
govern,458155,Verb
around,457570,Prep/Adv
possible,457233,Adj
head,456570,Noun
consider,456297,Verb
word,455831,Noun
program,454828,Noun
problem,453951,Noun
however,453479,Adv
lead,453063,Verb
system,452710,Noun
set,451873,Verb/Noun
order,450410,Noun/Verb
eye,449880,Noun
plan,449636,Noun/Verb
run,449024,Verb
keep,448369,Verb
face,447993,Noun
fact,447676,Noun
group,446932,Noun
play,446625,Verb
stand,445702,Verb
increase,444967,Verb
early,444626,Adj/Adv
course,444538,Noun
change,444351,Verb/Noun
help,442874,Verb
line,442801,Noun
EOF
    fi
fi

# 2. Download basic grammar patterns
echo "📝 Setting up grammar patterns..."
cat > "basic_grammar_patterns.json" << 'EOF'
{
  "sentence_patterns": [
    {"pattern": "Subject + Verb", "example": "Birds fly", "complexity": 1},
    {"pattern": "Subject + Verb + Object", "example": "Dogs chase cats", "complexity": 2},
    {"pattern": "Subject + Verb + Indirect Object + Direct Object", "example": "She gave him a book", "complexity": 3},
    {"pattern": "Subject + Linking Verb + Complement", "example": "The sky is blue", "complexity": 2},
    {"pattern": "There + Be + Subject", "example": "There are books on the table", "complexity": 3}
  ],
  "question_patterns": [
    {"pattern": "Wh-word + Auxiliary + Subject + Verb", "example": "What do you want?", "complexity": 3},
    {"pattern": "Auxiliary + Subject + Verb", "example": "Do you understand?", "complexity": 2},
    {"pattern": "Wh-word + Be + Subject", "example": "Where is the library?", "complexity": 2}
  ],
  "complex_structures": [
    {"pattern": "Relative Clause", "example": "The book that I read was interesting", "complexity": 4},
    {"pattern": "Conditional", "example": "If it rains, we will stay inside", "complexity": 4},
    {"pattern": "Passive Voice", "example": "The letter was written by John", "complexity": 3}
  ]
}
EOF

# 3. Create semantic relationship mappings
echo "🧠 Setting up semantic relationships..."
cat > "semantic_relationships.json" << 'EOF'
{
  "hypernyms": {
    "animal": ["dog", "cat", "bird", "fish", "mammal", "reptile"],
    "color": ["red", "blue", "green", "yellow", "purple", "orange"],
    "emotion": ["happy", "sad", "angry", "excited", "calm", "worried"],
    "food": ["apple", "bread", "meat", "vegetable", "fruit", "grain"]
  },
  "synonyms": {
    "big": ["large", "huge", "enormous", "gigantic"],
    "small": ["little", "tiny", "minute", "miniature"],
    "happy": ["joyful", "pleased", "content", "elated"],
    "sad": ["unhappy", "sorrowful", "melancholy", "dejected"]
  },
  "antonyms": {
    "hot": "cold",
    "big": "small",
    "happy": "sad",
    "fast": "slow",
    "light": "dark",
    "up": "down"
  },
  "part_of": {
    "tree": ["trunk", "branch", "leaf", "root"],
    "house": ["door", "window", "roof", "wall"],
    "car": ["wheel", "engine", "door", "window"],
    "body": ["head", "arm", "leg", "hand", "foot"]
  }
}
EOF

# 4. Create morphological patterns
echo "🔤 Setting up morphological patterns..."
cat > "morphology_patterns.json" << 'EOF'
{
  "prefixes": {
    "un-": {"meaning": "not, opposite", "examples": ["unhappy", "unfair", "unknown"]},
    "re-": {"meaning": "again", "examples": ["redo", "return", "rebuild"]},
    "pre-": {"meaning": "before", "examples": ["preview", "prepare", "predict"]},
    "dis-": {"meaning": "not, opposite", "examples": ["disagree", "disappear", "dislike"]}
  },
  "suffixes": {
    "-ing": {"meaning": "present participle", "examples": ["running", "singing", "dancing"]},
    "-ed": {"meaning": "past tense", "examples": ["walked", "talked", "played"]},
    "-er": {"meaning": "comparative", "examples": ["bigger", "faster", "stronger"]},
    "-est": {"meaning": "superlative", "examples": ["biggest", "fastest", "strongest"]},
    "-ly": {"meaning": "adverb", "examples": ["quickly", "slowly", "carefully"]},
    "-tion": {"meaning": "noun", "examples": ["action", "creation", "education"]}
  },
  "irregular_verbs": [
    {"base": "go", "past": "went", "past_participle": "gone"},
    {"base": "see", "past": "saw", "past_participle": "seen"},
    {"base": "take", "past": "took", "past_participle": "taken"},
    {"base": "give", "past": "gave", "past_participle": "given"}
  ]
}
EOF

# 5. Create age-of-acquisition data (based on research)
echo "👶 Setting up developmental data..."
cat > "age_of_acquisition.json" << 'EOF'
{
  "12_months": ["mama", "dada", "bye", "no", "up"],
  "18_months": ["more", "milk", "ball", "book", "shoe", "hat"],
  "24_months": ["eat", "drink", "sleep", "run", "go", "come", "big", "little"],
  "36_months": ["happy", "sad", "hot", "cold", "car", "house", "dog", "cat"],
  "48_months": ["because", "why", "when", "where", "yesterday", "tomorrow"],
  "60_months": ["remember", "forget", "think", "know", "believe", "understand"],
  "72_months": ["democracy", "cooperation", "responsibility", "imagination"]
}
EOF

echo "✅ External language resources setup complete!"
echo "📁 Resources available in: external_resources/"
echo "   • Word frequency lists"
echo "   • Grammar patterns"  
echo "   • Semantic relationships"
echo "   • Morphological patterns"
echo "   • Age-of-acquisition data"
echo ""
echo "💡 To use these in training, update ComprehensiveLanguageTrainer to load from these files"
