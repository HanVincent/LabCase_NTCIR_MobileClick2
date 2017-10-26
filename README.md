

# Lab Case - NTCIR MobileClick2
For my lab case, I participated in the MobileClick2 contest held by NTCIR, Japan.

### Introduction
We developed two different approaches to the MobileClick task. In the first approach, we add some extra processing to the baseline. The second approach is based on machine learning, which is very different from the first. Our system achieves an nDCG@3 score of 0.7415, nDCG@5 score of 0.764, nDCG@10 score of 0.8059, nDCG@20 score of 0.8732 and a Q-measure score of 0.9004, outperforming the baseline a little bit.

### Method
We categorized our methods into two kinds: Improved-Baseling and Machine Learning.

  1. Improved-Baseline:
     We did some extra processes on data and use the OddsRatio formula provided by the organizor.
    
    1.1 Natural Language Processing(NLP)
        Do stemming so as to get the correct quantity of words and remove meaningless words such as "the" and "his".
    1.2 Filter Infrequent Words
        Set a threshold, and filter those words, quantity of which are not up to the threshold, which means those words are too rare to be involved.
    1.3 Make negative scores to zero
        Ideally, the score should not be reduced by unimportant words. Instead, they should not matter.
    1.4 Take mean
        Overall, the longer iUnit is, the higher score is, which means comparing all iUnits in different length is unfair. Therefore, we divide score by the length of iUnit.	
    1.5 Ranks of Page
	      Documents are derived from Bing search, which possesses its own searching algorithm. Based on the rank sorted by Bing, we give it a weight. Word appears in top pages will occupy more score, vice versa.
    1.6 Do not use smoothing
	      Before using the baseline, we need to know what will happen without smoothing. Therefore, we take a try to see what will happen if there is no smoothing done.

  2. Machine Learning:
     Predict the answer through the relation between query and iUnits with the tool Liblinear-SVM and the 100-dimension Word2Vec dictionary pre-trained by Wikipedia 2014 corpus and English Gigaword fifth edition corpus. Each word has its 100-value vector, and closer the distance between two words, more frequently they appear together.

    2.1 Pointwise
	      Taking the ground truth of training data as label, we set 101 values as features composed of the 100-dimension vector and the score from the Improved-Baseline method. We evaluated our model with 5-fold.
	      
    2.2 Pairwise
	       Considering the relation between iUnits, we divide labels into three kinds: 0(larger), 1(equal), and 2(smaller). There are 201 features for each instance, vector of iUnit A - Q and iUnit B - Q. The last one is the score of iUnit A minus that of iUnit B gained from Improved-Baseline.

### Conclusion
As the table presenting, for Improved Methods with filtering infrequent words and removing smoothing, we can get the score 0.9004 higher than the score of baseline. For Machine Learning with the pairwise method, although it is just higher a little bit than baseline, there is room to improve.
