# Used internally by the THE() function.
zzzz-the = { PROPER($gender) ->
    *[false] the { $gender }
     [true] { $gender }
    }

# Used internally by the SUBJECT() function.
zzzz-subject-pronoun = { $gender ->
    [male] he
    [female] she
    [epicene] they
   *[neuter] it
   }

# Used internally by the OBJECT() function.
zzzz-object-pronoun = { $gender ->
    [male] him
    [female] her
    [epicene] them
   *[neuter] it
   }

# Used internally by the DAT-OBJ() function.
# Not used in en-US. Created to support other languages.
# (e.g., "to him," "for her")
zzzz-dat-object = { $gender ->
    [male] him
    [female] her
    [epicene] them
   *[neuter] it
   }

# Used internally by the GENITIVE() function.
# Not used in en-US. Created to support other languages.
# e.g., "у него" (Russian), "seines Vaters" (German).
zzzz-genitive = { $gender ->
    [male] his
    [female] her
    [epicene] their
   *[neuter] its
   }

# Used internally by the POSS-PRONOUN() function.
zzzz-possessive-pronoun = { $gender ->
    [male] his
    [female] hers
    [epicene] theirs
   *[neuter] its
   }

# Used internally by the POSS-ADJ() function.
zzzz-possessive-adjective = { $gender ->
    [male] his
    [female] her
    [epicene] their
   *[neuter] its
   }

# Used internally by the REFLEXIVE() function.
zzzz-reflexive-pronoun = { $gender ->
    [male] himself
    [female] herself
    [epicene] themselves
   *[neuter] itself
   }

# Used internally by the CONJUGATE-BE() function.
zzzz-conjugate-be = { $gender ->
    [epicene] are
   *[other] is
   }

# Used internally by the CONJUGATE-HAVE() function.
zzzz-conjugate-have = { $gender ->
    [epicene] have
   *[other] has
   }

# Used internally by the CONJUGATE-BASIC() function.
zzzz-conjugate-basic = { $gender ->
    [epicene] { $first }
   *[other] { $second }
   }
