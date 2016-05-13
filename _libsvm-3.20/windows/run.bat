rem ¥ýscale ¦Atrain ¦Apredict
FOR /L %%k IN (0,1,4) DO (

	svm-scale.exe -l 0 -s %%k_scale %%k_training.txt > %%k_train_s.txt
	svm-scale.exe -r %%k_scale %%k_test.txt > %%k_test_s.txt
		
	svm-train.exe -s 3 -c 1000 -t 2 %%k_train_s.txt model
	svm-predict.exe %%k_test_s.txt model %%k_result.out
)

copy /b *.out result.txt
copy /b *_test_s.txt ans.txt
pause
